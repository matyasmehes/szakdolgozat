using Microsoft.Identity.Client;

namespace Szakdoga.Models
{ 
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
