using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    public enum RoomType
    {
        Single,
        Double,
        Studio,
        Shared,
        Duplex,
        Apartment,  // Khớp với SQL seed data ('Apartment')
        Other
    }
}
