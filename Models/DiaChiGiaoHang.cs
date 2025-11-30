using System;
using System.Collections.Generic;

namespace ShopNgocLan.Models;

public partial class DiaChiGiaoHang
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string DiaChiChiTiet { get; set; } = null!;

    public string PhuongXa { get; set; } = null!;

    public string QuanHuyen { get; set; } = null!;

    public string TinhThanhPho { get; set; } = null!;

    public bool? LaMacDinh { get; set; }

    public virtual User User { get; set; } = null!;
}
