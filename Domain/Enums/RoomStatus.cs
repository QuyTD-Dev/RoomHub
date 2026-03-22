using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    public enum RoomStatus
    {
        Available,
        Deposited,
        Occupied,
        Maintenance,         // Khớp với SQL seed data ('Maintenance')
        UnderMaintenance,    // Giữ lại để backward compat
        Active,
        Hidden
    }
}
