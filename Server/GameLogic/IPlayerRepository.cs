using Shared.GameLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.GameLogic
{
    public interface IPlayerRepository
    {
        Task AddAsync(PlayerData playerData);
        Task UpdateAsync(PlayerData playerData);

        Task<PlayerData[]> GetAll();
    }
}
