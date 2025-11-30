using System;
using System.Collections.Generic;

namespace ShopNgocLan.Models;

public partial class Voucher
{
    public int Id { get; set; }

    public string MaVoucher { get; set; } = null!;

    public string? MoTa { get; set; }

    public int LoaiGiamGia { get; set; }

    public decimal GiaTriGiam { get; set; }

    public decimal? GiamGiaToiDa { get; set; }

    public decimal DonHangToiThieu { get; set; }

    public DateTime NgayBatDau { get; set; }

    public DateTime NgayKetThuc { get; set; }

    public int? SoLuotSuDungToiDa { get; set; }

    public int SoLuotDaSuDung { get; set; }

    public bool KichHoat { get; set; }
}
