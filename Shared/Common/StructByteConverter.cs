using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

public class StructByteConverter
{
    //public static T ToStruct<T>(byte[] bytes, int offset = 0) where T : struct
    //{
    //    try 
    //    {
    //        // Calculate size of the struct T
    //        int size = Marshal.SizeOf<T>();

    //        // Validate input length
    //        if (bytes.Length - offset < size)
    //            throw new ArgumentException($"Byte array {bytes.Length - offset} is too small for struct {typeof(T).Name}. Need {size} bytes.");

    //        return MemoryMarshal.Read<T>(bytes.AsSpan(offset));
    //    }
    //    catch (Exception ex) 
    //    {
    //        Logger.Log($"{ex.Message}", ELogLevel.Error);
    //        return default(T);  
    //    }

            
    //}
    
    public static T ToStruct<T>(byte[] bytes, int offset = 0) where T : struct
    {
        GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            IntPtr ptr = handle.AddrOfPinnedObject();
            return Marshal.PtrToStructure<T>(ptr + offset);
        }
        finally
        {
            handle.Free();
        }


    }

    /// <summary>
    /// Convert any struct into a byte array by iterating its fields.
    /// </summary>
    public static byte[] ToBytes<T>(T value)
    {

        //int size = Marshal.SizeOf<T>();
        //byte[] bytes = new byte[size];

        //GCHandle handle = GCHandle.Alloc(value, GCHandleType.Pinned);
        //try
        //{
        //    IntPtr ptr = handle.AddrOfPinnedObject();
        //    Marshal.Copy(ptr, bytes, 0, size);
        //}
        //finally
        //{
        //    handle.Free();
        //}

        //return bytes;

        MemoryStream stream = new MemoryStream();

        try
        {
            // Get all fields (public + private) in declared order.
            var fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            for (int i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                object? fieldValue = field.GetValue(value);

                // When current field is an array, try to get length from the previous (period) field (e.g. UsernameSize -> Username).
                //int? arrayLength = null;
                //if (field.FieldType.IsArray && i > 0)
                //{
                //    var prevField = fields[i - 1];
                //    if ((prevField.FieldType == typeof(int) || prevField.FieldType == typeof(uint)) &&
                //        prevField.Name.EndsWith("Size", StringComparison.OrdinalIgnoreCase))
                //    {
                //        var prevVal = prevField.GetValue(value);
                //        if (prevVal != null)
                //            arrayLength = prevField.FieldType == typeof(uint) ? (int)(uint)prevVal : (int)prevVal;
                //    }
                //}

                byte[] fieldBytes = ConvertFieldToBytes(fieldValue, field, null);
                stream.Write(fieldBytes, 0, fieldBytes.Length);
            }

        }
        catch (Exception e)
        {
            Logger.Log(e.Message, ELogLevel.Error);
        }
                

        return stream.ToArray();
    }
    /// <summary>
    /// Convert a single field value into bytes depending on its type.
    /// </summary>
    /// <param name="value">Field value.</param>
    /// <param name="field">Field info.</param>
    /// <param name="arrayLength">Optional length for array fields (from preceding *Size field).</param>
    private static byte[] ConvertFieldToBytes(object? value, FieldInfo field, int? arrayLength = null)
    {
        if (value == null)
            return Array.Empty<byte>();

        Type type = value.GetType();

        // ────────────── ARRAY (length from previous *Size field) ──────────────
        if (type.IsArray)
        {
            Array arr = (Array)value;
            int count = arrayLength ?? arr.Length;
            Type elemType = type.GetElementType()!;

            if (elemType == typeof(byte))
            {
                return NormalizeByteArray((byte[])value, count);
            }

            // Other element types: convert each element up to count
            var list = new List<byte>();
            int n = Math.Min(count, arr.Length);
            for (int i = 0; i < n; i++)
            {
                object elem = arr.GetValue(i)!;
                byte[] elemBytes = ToBytes(elem);
                list.AddRange(elemBytes);
            }
            return list.ToArray();
        }

        // ────────────── FIXED BUFFER (e.g., fixed byte[128]) ──────────────
        // Fixed buffers are compiler-generated nested structs
        //if (type.IsValueType && type.Name.Contains('<'))
        //{
        //    var fixedBufferAttr = field.GetCustomAttribute<FixedBufferAttribute>();
        //    if (fixedBufferAttr != null)
        //    {
        //        int bufferSize = fixedBufferAttr.Length;
        //        byte[] buffer = new byte[bufferSize];

        //        // Use unsafe code to copy the fixed buffer
        //        unsafe
        //        {
        //            fixed (byte* dest = buffer)
        //            {
        //                // Get pointer to the fixed buffer field
        //                System.Runtime.CompilerServices.Unsafe.CopyBlock(
        //                    dest,
        //                    System.Runtime.CompilerServices.Unsafe.AsPointer(ref value),
        //                    (uint)bufferSize
        //                );
        //            }
        //        }

        //        return buffer;
        //    }
        //}

        // ────────────── PRIMITIVE TYPES ──────────────
        if (type == typeof(byte)) return new[] { (byte)value };
        if (type == typeof(bool)) return BitConverter.GetBytes((bool)value);
        if (type == typeof(short)) return BitConverter.GetBytes((short)value);
        if (type == typeof(ushort)) return BitConverter.GetBytes((ushort)value);
        if (type == typeof(int)) return BitConverter.GetBytes((int)value);
        if (type == typeof(uint)) return BitConverter.GetBytes((uint)value);
        if (type == typeof(long)) return BitConverter.GetBytes((long)value);
        if (type == typeof(ulong)) return BitConverter.GetBytes((ulong)value);
        if (type == typeof(float)) return BitConverter.GetBytes((float)value);
        if (type == typeof(double)) return BitConverter.GetBytes((double)value);

    // ────────────── BYTE ARRAY ──────────────
    if (type == typeof(byte[]))
    {
        int sizeConst = GetMarshalSizeConst(field);
        if (sizeConst > 0)
            return NormalizeByteArray((byte[])value, sizeConst);

        return (byte[])value;
    }

    // ────────────── STRING ──────────────
    if (type == typeof(string))
        {
            string s = (string)value;

            // Convert string to UTF8 and prefix with length (optional)
            byte[] strBytes = Encoding.UTF8.GetBytes(s);
            byte[] lengthBytes = BitConverter.GetBytes((ushort)strBytes.Length);

            return lengthBytes.Concat(strBytes).ToArray();
        }

        // ────────────── NESTED STRUCT ──────────────
        if (type.IsValueType && !type.IsPrimitive)
        {
            var toBytesMethod = typeof(StructByteConverter).GetMethods()
                .FirstOrDefault(m => m.Name == nameof(ToBytes) && m.IsGenericMethodDefinition && m.GetGenericArguments().Length == 1);
            if (toBytesMethod == null)
                throw new NotSupportedException($"Could not find ToBytes method for type: {type}");
            var genericMethod = toBytesMethod.MakeGenericMethod(type);
            var result = genericMethod.Invoke(null, new[] { value });
            return result is byte[] bytes ? bytes : Array.Empty<byte>();
        }

        throw new NotSupportedException($"Unsupported field type: {type}");
    }


    private static int GetMarshalSizeConst(Type structType, string fieldName)
    {
        var field = structType.GetField(fieldName);
        if (field == null) return -1;

        var attr = field.GetCustomAttributes(typeof(MarshalAsAttribute), false)
                        .FirstOrDefault() as MarshalAsAttribute;

        return attr?.SizeConst ?? -1;
    }

    private static int GetMarshalSizeConst(FieldInfo field)
    {
        if (field == null) return -1;

        var attr = field.GetCustomAttributes(typeof(MarshalAsAttribute), false)
                        .FirstOrDefault() as MarshalAsAttribute;

        return attr?.SizeConst ?? -1;
    }    


    private static byte[] NormalizeByteArray(byte[]? value, int size)
    {
        var result = new byte[size];

        if (value == null)
            return result;

        int copyLen = Math.Min(size, value.Length);
        Array.Copy(value, result, copyLen);

        return result;
    }



}
