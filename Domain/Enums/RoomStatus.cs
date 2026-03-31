using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations;

namespace Domain.Enums
{
    public enum RoomStatus
    {
        [Display(Name = "Sẵn sàng")]
        Available,

        [Display(Name = "Đã đặt cọc")]
        Deposited,

        [Display(Name = "Đã có người ở")]
        Occupied,

        [Display(Name = "Bảo trì")]
        Maintenance, // match DB

        [Display(Name = "Đang bảo trì")]
        UnderMaintenance, // backward compat

        [Display(Name = "Đang hoạt động")]
        Active,

        [Display(Name = "Đã ẩn")]
        Hidden,

        [Display(Name = "Chờ xác nhận")]
        PendingApproval
    }
}


