using Microsoft.Identity.Client;

namespace Szakdoga.Models
{
    public class MenuItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }

    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerAddress { get; set; }
        public decimal TotalPrice { get; set; }
        public bool IsDelivered { get; set; }
        public List<OrderItem> OrderItems { get; set; }
        public DateTime OrderDate { get; set; }

        public virtual User User { get; set; }
        
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public MenuItem MenuItem { get; set; }
        public int Quantity { get; set; }
    }


    public class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public byte[] Salt { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual ICollection<Order> Orders { get; set; }
    }

    public class OrderRequest
    {
        public string CustomerPhone { get; set; }
        public string CustomerAddress { get; set; }
        public List<OrderItem> OrderItems { get; set; }
    }

    public class RegisterRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    



}
