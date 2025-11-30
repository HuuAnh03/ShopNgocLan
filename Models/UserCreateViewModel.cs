namespace ShopNgocLan.Models
{
    public class UserCreateViewModel
    {
        public User User { get; set; } = new User();
        public List<Role> RolesList { get; set; } = new List<Role>();
    }
}
