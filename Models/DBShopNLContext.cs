using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ShopNgocLan.Models;

public partial class DBShopNLContext : DbContext
{
    public DBShopNLContext()
    {
    }

    public DBShopNLContext(DbContextOptions<DBShopNLContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ChatConversation> ChatConversations { get; set; }

    public virtual DbSet<ChatIntent> ChatIntents { get; set; }

    public virtual DbSet<ChatIntentPattern> ChatIntentPatterns { get; set; }

    public virtual DbSet<ChatIntentReply> ChatIntentReplies { get; set; }

    public virtual DbSet<ChatMessage> ChatMessages { get; set; }

    public virtual DbSet<ChiTietGioHang> ChiTietGioHangs { get; set; }

    public virtual DbSet<ChiTietHoaDon> ChiTietHoaDons { get; set; }

    public virtual DbSet<ChiTietSanPham> ChiTietSanPhams { get; set; }

    public virtual DbSet<DanhGiaSanPham> DanhGiaSanPhams { get; set; }

    public virtual DbSet<DanhMucSanPham> DanhMucSanPhams { get; set; }

    public virtual DbSet<DiaChiGiaoHang> DiaChiGiaoHangs { get; set; }

    public virtual DbSet<GioHang> GioHangs { get; set; }

    public virtual DbSet<HinhAnhSanPham> HinhAnhSanPhams { get; set; }

    public virtual DbSet<HoaDon> HoaDons { get; set; }

    public virtual DbSet<KhuyenMai> KhuyenMais { get; set; }

    public virtual DbSet<LichSuTimKiem> LichSuTimKiems { get; set; }

    public virtual DbSet<LichSuTonKho> LichSuTonKhos { get; set; }

    public virtual DbSet<MauSac> MauSacs { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<PhuongThucThanhToan> PhuongThucThanhToans { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SanPham> SanPhams { get; set; }

    public virtual DbSet<SanPhamKhuyenMai> SanPhamKhuyenMais { get; set; }

    public virtual DbSet<Size> Sizes { get; set; }

    public virtual DbSet<TinTuc> TinTucs { get; set; }

    public virtual DbSet<TrangThaiDonHang> TrangThaiDonHangs { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Voucher> Vouchers { get; set; }

    public virtual DbSet<Wishlist> Wishlists { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=HUUANH;Initial Catalog=DBShopNL;Persist Security Info=True;User ID=huuanh;Password=123456;Encrypt=True;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChatConversation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ChatConv__3214EC0782599CEC");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IsBotActive).HasDefaultValue(true);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Open");
            entity.Property(e => e.Subject).HasMaxLength(200);

            entity.HasOne(d => d.AssignedStaff).WithMany(p => p.ChatConversationAssignedStaffs)
                .HasForeignKey(d => d.AssignedStaffId)
                .HasConstraintName("FK_ChatConversations_AssignedStaff");

            entity.HasOne(d => d.Customer).WithMany(p => p.ChatConversationCustomers)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ChatConversations_Customer");
        });

        modelBuilder.Entity<ChatIntent>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ChatInte__3214EC0756515D95");

            entity.ToTable("ChatIntent");

            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.MoTa).HasMaxLength(500);
            entity.Property(e => e.TenIntent).HasMaxLength(200);
            entity.Property(e => e.TrangThai).HasDefaultValue(true);
        });

        modelBuilder.Entity<ChatIntentPattern>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ChatInte__3214EC078F392A2C");

            entity.ToTable("ChatIntentPattern");

            entity.Property(e => e.PatternText).HasMaxLength(200);
            entity.Property(e => e.TrangThai).HasDefaultValue(true);

            entity.HasOne(d => d.Intent).WithMany(p => p.ChatIntentPatterns)
                .HasForeignKey(d => d.IntentId)
                .HasConstraintName("FK_ChatIntentPattern_ChatIntent");
        });

        modelBuilder.Entity<ChatIntentReply>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ChatInte__3214EC07673631B2");

            entity.ToTable("ChatIntentReply");

            entity.Property(e => e.TrangThai).HasDefaultValue(true);

            entity.HasOne(d => d.Intent).WithMany(p => p.ChatIntentReplies)
                .HasForeignKey(d => d.IntentId)
                .HasConstraintName("FK_ChatIntentReply_ChatIntent");
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ChatMess__3214EC0702DAE053");

            entity.Property(e => e.SenderType)
                .HasMaxLength(20)
                .HasDefaultValue("Customer");
            entity.Property(e => e.SentAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Conversation).WithMany(p => p.ChatMessages)
                .HasForeignKey(d => d.ConversationId)
                .HasConstraintName("FK_ChatMessages_Conversation");

            entity.HasOne(d => d.Sender).WithMany(p => p.ChatMessages)
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ChatMessages_Sender");
        });

        modelBuilder.Entity<ChiTietGioHang>(entity =>
        {
            entity.HasKey(e => e.ChiTietGioHangId).HasName("PK__ChiTietG__53F4D8F48EF6BDBA");

            entity.ToTable("ChiTietGioHang");

            entity.HasOne(d => d.ChiTietSanPham).WithMany(p => p.ChiTietGioHangs)
                .HasForeignKey(d => d.ChiTietSanPhamId)
                .HasConstraintName("FK__ChiTietGi__ChiTi__619B8048");

            entity.HasOne(d => d.GioHang).WithMany(p => p.ChiTietGioHangs)
                .HasForeignKey(d => d.GioHangId)
                .HasConstraintName("FK__ChiTietGi__GioHa__60A75C0F");
        });

        modelBuilder.Entity<ChiTietHoaDon>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ChiTietH__3214EC076F0A917E");

            entity.ToTable("ChiTietHoaDon");

            entity.Property(e => e.DonGia).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.ChiTietSanPham).WithMany(p => p.ChiTietHoaDons)
                .HasForeignKey(d => d.ChiTietSanPhamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ChiTietHo__ChiTi__6E01572D");

            entity.HasOne(d => d.HoaDon).WithMany(p => p.ChiTietHoaDons)
                .HasForeignKey(d => d.HoaDonId)
                .HasConstraintName("FK__ChiTietHo__HoaDo__6D0D32F4");
        });

        modelBuilder.Entity<ChiTietSanPham>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ChiTietS__3214EC07F4B04BCD");

            entity.ToTable("ChiTietSanPham");

            entity.HasIndex(e => new { e.SanPhamId, e.MauSacId, e.SizeId }, "UQ_SanPham_MauSac_Size").IsUnique();

            entity.Property(e => e.Gia).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.GiaGoc).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.GiaNhap).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.MauSac).WithMany(p => p.ChiTietSanPhams)
                .HasForeignKey(d => d.MauSacId)
                .HasConstraintName("FK_ChiTietSanPham_MauSac");

            entity.HasOne(d => d.SanPham).WithMany(p => p.ChiTietSanPhams)
                .HasForeignKey(d => d.SanPhamId)
                .HasConstraintName("FK__ChiTietSa__SanPh__52593CB8");

            entity.HasOne(d => d.Size).WithMany(p => p.ChiTietSanPhams)
                .HasForeignKey(d => d.SizeId)
                .HasConstraintName("FK_ChiTietSanPham_Size");
        });

        modelBuilder.Entity<DanhGiaSanPham>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DanhGiaS__3214EC070AF4DDF1");

            entity.ToTable("DanhGiaSanPham");

            entity.Property(e => e.IsPublished).HasDefaultValue(true);
            entity.Property(e => e.NgayDanhGia)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.NoiDung).HasColumnType("ntext");

            entity.HasOne(d => d.AdminUser).WithMany(p => p.DanhGiaSanPhamAdminUsers)
                .HasForeignKey(d => d.AdminUserId)
                .HasConstraintName("FK_DanhGiaSanPham_AdminUser");

            entity.HasOne(d => d.SanPham).WithMany(p => p.DanhGiaSanPhams)
                .HasForeignKey(d => d.SanPhamId)
                .HasConstraintName("FK__DanhGiaSa__SanPh__72C60C4A");

            entity.HasOne(d => d.User).WithMany(p => p.DanhGiaSanPhamUsers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__DanhGiaSa__UserI__73BA3083");
        });

        modelBuilder.Entity<DanhMucSanPham>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DanhMucS__3214EC0791F30234");

            entity.ToTable("DanhMucSanPham");

            entity.Property(e => e.Path).IsUnicode(false);
            entity.Property(e => e.TenDanhMuc).HasMaxLength(255);

            entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent)
                .HasForeignKey(d => d.ParentId)
                .HasConstraintName("FK__DanhMucSa__Paren__440B1D61");
        });

        modelBuilder.Entity<DiaChiGiaoHang>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DiaChiGi__3214EC072527B7FB");

            entity.ToTable("DiaChiGiaoHang");

            entity.Property(e => e.DiaChiChiTiet).HasMaxLength(255);
            entity.Property(e => e.LaMacDinh).HasDefaultValue(false);
            entity.Property(e => e.PhuongXa).HasMaxLength(100);
            entity.Property(e => e.QuanHuyen).HasMaxLength(100);
            entity.Property(e => e.TinhThanhPho).HasMaxLength(100);

            entity.HasOne(d => d.User).WithMany(p => p.DiaChiGiaoHangs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__DiaChiGia__UserI__412EB0B6");
        });

        modelBuilder.Entity<GioHang>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GioHang__3214EC07A4841BC3");

            entity.ToTable("GioHang");

            entity.HasIndex(e => e.UserId, "UQ__GioHang__1788CC4DDBAB3475").IsUnique();

            entity.HasOne(d => d.User).WithOne(p => p.GioHang)
                .HasForeignKey<GioHang>(d => d.UserId)
                .HasConstraintName("FK__GioHang__UserId__5CD6CB2B");
        });

        modelBuilder.Entity<HinhAnhSanPham>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__HinhAnhS__3214EC07AA7B277D");

            entity.ToTable("HinhAnhSanPham");

            entity.Property(e => e.LaAnhDaiDien).HasDefaultValue(false);

            entity.HasOne(d => d.MauSac).WithMany(p => p.HinhAnhSanPhams)
                .HasForeignKey(d => d.MauSacId)
                .HasConstraintName("FK_HinhAnhSanPham_MauSac");

            entity.HasOne(d => d.SanPham).WithMany(p => p.HinhAnhSanPhams)
                .HasForeignKey(d => d.SanPhamId)
                .HasConstraintName("FK__HinhAnhSa__SanPh__4CA06362");
        });

        modelBuilder.Entity<HoaDon>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__HoaDon__3214EC072B8118FE");

            entity.ToTable("HoaDon");

            entity.Property(e => e.EmailKhachHang).HasMaxLength(255);
            entity.Property(e => e.GiamGia)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PhiVanChuyen)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SoDienThoaiKhachHang).HasMaxLength(15);
            entity.Property(e => e.TenKhachHang).HasMaxLength(100);
            entity.Property(e => e.ThanhTien).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TongTien).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.PhuongThucThanhToan).WithMany(p => p.HoaDons)
                .HasForeignKey(d => d.PhuongThucThanhToanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__HoaDon__PhuongTh__6A30C649");

            entity.HasOne(d => d.TrangThai).WithMany(p => p.HoaDons)
                .HasForeignKey(d => d.TrangThaiId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HoaDon_TrangThai");

            entity.HasOne(d => d.User).WithMany(p => p.HoaDons)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__HoaDon__UserId__693CA210");
        });

        modelBuilder.Entity<KhuyenMai>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__KhuyenMa__3214EC07C9C5BB5D");

            entity.ToTable("KhuyenMai");

            entity.Property(e => e.MoTa).HasColumnType("ntext");
            entity.Property(e => e.NgayBatDau).HasColumnType("datetime");
            entity.Property(e => e.NgayKetThuc).HasColumnType("datetime");
            entity.Property(e => e.PhanTramGiam).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.TenKhuyenMai).HasMaxLength(255);
        });

        modelBuilder.Entity<LichSuTimKiem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__LichSuTi__3214EC074796D427");

            entity.ToTable("LichSuTimKiem");

            entity.Property(e => e.ThoiGianTimKiem)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TuKhoa).HasMaxLength(255);

            entity.HasOne(d => d.User).WithMany(p => p.LichSuTimKiems)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__LichSuTim__UserI__00200768");
        });

        modelBuilder.Entity<LichSuTonKho>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__LichSuTo__3214EC07B8ADB74E");

            entity.ToTable("LichSuTonKho");

            entity.Property(e => e.GhiChu).HasMaxLength(500);
            entity.Property(e => e.LoaiGiaoDich).HasMaxLength(50);
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.ChiTietSanPham).WithMany(p => p.LichSuTonKhos)
                .HasForeignKey(d => d.ChiTietSanPhamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LichSuTonKho_ChiTietSanPham");

            entity.HasOne(d => d.NhanVien).WithMany(p => p.LichSuTonKhos)
                .HasForeignKey(d => d.NhanVienId)
                .HasConstraintName("FK_LichSuTonKho_User");
        });

        modelBuilder.Entity<MauSac>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__MauSac__3214EC072C04D841");

            entity.ToTable("MauSac");

            entity.HasIndex(e => e.TenMau, "UQ__MauSac__332F6A919BB6678F").IsUnique();

            entity.Property(e => e.MaMauHex).HasMaxLength(7);
            entity.Property(e => e.TenMau).HasMaxLength(50);
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Permissi__3214EC074D77A700");

            entity.HasIndex(e => e.Name, "UQ__Permissi__737584F6FABE2E74").IsUnique();

            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<PhuongThucThanhToan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__PhuongTh__3214EC07AD85B042");

            entity.ToTable("PhuongThucThanhToan");

            entity.Property(e => e.TenPhuongThuc).HasMaxLength(100);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Roles__3214EC07CC785F32");

            entity.HasIndex(e => e.TenRole, "UQ__Roles__37A723F36C69DC0C").IsUnique();

            entity.Property(e => e.TenRole).HasMaxLength(50);

            entity.HasMany(d => d.Permissions).WithMany(p => p.Roles)
                .UsingEntity<Dictionary<string, object>>(
                    "RolePermission",
                    r => r.HasOne<Permission>().WithMany()
                        .HasForeignKey("PermissionId")
                        .HasConstraintName("FK_RolePermissions_Permissions"),
                    l => l.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .HasConstraintName("FK_RolePermissions_Roles"),
                    j =>
                    {
                        j.HasKey("RoleId", "PermissionId").HasName("PK__RolePerm__6400A1A80DE93FE2");
                        j.ToTable("RolePermissions");
                    });
        });

        modelBuilder.Entity<SanPham>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SanPham__3214EC07E3444A09");

            entity.ToTable("SanPham");

            entity.Property(e => e.ChatLieu).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MoTa).HasColumnType("ntext");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TenSanPham).HasMaxLength(255);
            entity.Property(e => e.ThuongHieu).HasMaxLength(100);

            entity.HasOne(d => d.DanhMuc).WithMany(p => p.SanPhams)
                .HasForeignKey(d => d.DanhMucId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SanPham__DanhMuc__48CFD27E");
        });

        modelBuilder.Entity<SanPhamKhuyenMai>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SanPham___6D38D89D5BF2735D");

            entity.ToTable("SanPhamKhuyenMai");

            entity.HasOne(d => d.KhuyenMai).WithMany(p => p.SanPhamKhuyenMais)
                .HasForeignKey(d => d.KhuyenMaiId)
                .HasConstraintName("FK__SanPham_K__Khuye__59063A47");

            entity.HasOne(d => d.SanPham).WithMany(p => p.SanPhamKhuyenMais)
                .HasForeignKey(d => d.SanPhamId)
                .HasConstraintName("FK__SanPham_K__SanPh__5812160E");
        });

        modelBuilder.Entity<Size>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Size__3214EC0774EAB10B");

            entity.ToTable("Size");

            entity.HasIndex(e => e.TenSize, "UQ__Size__C86AACB965AFA478").IsUnique();

            entity.Property(e => e.TenSize).HasMaxLength(50);
        });

        modelBuilder.Entity<TinTuc>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TinTuc__3214EC0740DBBB08");

            entity.ToTable("TinTuc");

            entity.Property(e => e.ExternalUrl).HasMaxLength(500);
            entity.Property(e => e.MoTaNgan).HasMaxLength(300);
            entity.Property(e => e.NgayDang)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.NoiDung).HasColumnType("ntext");
            entity.Property(e => e.Slug).HasMaxLength(255);
            entity.Property(e => e.TrangThai).HasDefaultValue(true);

            entity.HasOne(d => d.TacGia).WithMany(p => p.TinTucs)
                .HasForeignKey(d => d.TacGiaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TinTuc__TacGiaId__778AC167");
        });

        modelBuilder.Entity<TrangThaiDonHang>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TrangTha__3214EC0783888590");

            entity.ToTable("TrangThaiDonHang");

            entity.HasIndex(e => e.MaTrangThai, "UQ__TrangTha__AADE41394C7BBEBD").IsUnique();

            entity.Property(e => e.MaTrangThai)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TenTrangThai).HasMaxLength(100);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC07AFA41103");

            entity.HasIndex(e => e.SoDienThoai, "UQ__Users__0389B7BD543027A4").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Users__A9D10534EDD6BB20").IsUnique();

            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.GioiTinh).HasMaxLength(10);
            entity.Property(e => e.Ho).HasMaxLength(50);
            entity.Property(e => e.MatKhau).HasMaxLength(255);
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SoDienThoai).HasMaxLength(15);
            entity.Property(e => e.Ten).HasMaxLength(50);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Users__RoleId__3D5E1FD2");
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Voucher__3214EC0783239F63");

            entity.ToTable("Voucher");

            entity.HasIndex(e => e.MaVoucher, "UQ__Voucher__0AAC5B10659AD0A5").IsUnique();

            entity.Property(e => e.DonHangToiThieu).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.GiaTriGiam).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.GiamGiaToiDa).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.KichHoat).HasDefaultValue(true);
            entity.Property(e => e.MaVoucher).HasMaxLength(50);
            entity.Property(e => e.MoTa).HasMaxLength(255);
            entity.Property(e => e.NgayBatDau).HasColumnType("datetime");
            entity.Property(e => e.NgayKetThuc).HasColumnType("datetime");
        });

        modelBuilder.Entity<Wishlist>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.SanPhamId }).HasName("PK__Wishlist__47D94CB11D206155");

            entity.ToTable("Wishlist");

            entity.Property(e => e.NgayThem)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.SanPham).WithMany(p => p.Wishlists)
                .HasForeignKey(d => d.SanPhamId)
                .HasConstraintName("FK__Wishlist__SanPha__7C4F7684");

            entity.HasOne(d => d.User).WithMany(p => p.Wishlists)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Wishlist__UserId__7B5B524B");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
