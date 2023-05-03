using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Linq;
using Szakdoga.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;

namespace Szakdoga.Controllers
{

    
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly MyDbContext _context;
        private readonly IConfiguration _configuration;

        public OrdersController(MyDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
        {
            var orders = await _context.Orders
                    .Where(o => !o.IsDelivered)
                    .Include(o => o.OrderItems).ThenInclude(oi => oi.MenuItem).Include(o => o.User)
                    .Select(o => new
                    {
                        o.Id,
                        o.UserId,
                        CustomerName = o.User.FirstName + " " + o.User.LastName,
                        o.CustomerPhone,
                        o.CustomerAddress,
                        o.TotalPrice,
                        o.IsDelivered,
                        o.OrderItems,
                        o.OrderDate
                    })
                    .ToListAsync();
            return Ok(orders);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            var order = await _context.Orders.Include(o => o.OrderItems).ThenInclude(oi => oi.MenuItem).FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return Ok(order);
        }

        [HttpGet]
        [Route("/api/menuitems")]
        public async Task<ActionResult<IEnumerable<MenuItem>>> GetMenuItems()
        {
            var menuItems = await _context.MenuItems.ToListAsync();
            return Ok(menuItems);
        }

        [Authorize]
        [HttpPost("postorder")]
        [Route("/api/Order")]
        public async Task<IActionResult> PostOrder([FromBody] OrderRequest orderRequest)
        {
            DateTime orderDate = DateTime.Now;
            if (orderRequest == null)
            {
                return BadRequest("Order data is missing");
            }
            var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return BadRequest("Invalid token");
            }
            int userId = int.Parse(userIdClaim.Value);
            Order order = new Order
            {
                UserId = userId,
                CustomerPhone = orderRequest.CustomerPhone,
                CustomerAddress = orderRequest.CustomerAddress,
                OrderItems = orderRequest.OrderItems,
                IsDelivered = false,
                OrderDate = orderDate
            };
            order.TotalPrice = CalculateTotalPrice(order.OrderItems);

            foreach (var orderItem in order.OrderItems)
            {
                var existingMenuItem = _context.MenuItems.FirstOrDefault(mi => mi.Id == orderItem.MenuItem.Id);
                if (existingMenuItem != null)
                {
                    orderItem.MenuItem = existingMenuItem;
                }
                else break;
            }

            _context.Orders.Add(order);
            _context.SaveChanges();

            return Ok("Order has been placed successfully");
        }

        private decimal CalculateTotalPrice(List<OrderItem> orderItems)
        {
            decimal totalPrice = 0;
            foreach (var orderItem in orderItems)
            {
                totalPrice += orderItem.Quantity * orderItem.MenuItem.Price;
            }
            return totalPrice;
        }

        [HttpPost("login")]
        [Route("/api/login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                return Unauthorized();
            }

            if (!VerifyPasswordHash(request.Password, user.PasswordHash, user.Salt))
            {
                return Unauthorized();
            }

            var token = GenerateJwtToken(user);

            return Ok(new { token });
        }

        private bool VerifyPasswordHash(string password, string passwordHash, byte[] salt)
        {
            byte[] storedHash = Convert.FromBase64String(passwordHash);

            using var hmac = new HMACSHA512(salt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            for (int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != storedHash[i]) return false;
            }
            return true;
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration.GetSection("JwtConfig")["Secret"]);
            Console.WriteLine(user.ToString());
            var claimsIdentity = new ClaimsIdentity();
            claimsIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
            claimsIdentity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
            var claimsDescripitor = new SecurityTokenDescriptor
            {
                Subject = claimsIdentity,
                Expires = DateTime.UtcNow.AddDays(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = "myapp",
                Audience = "myclient"
            };
            Console.WriteLine(user.ToString());
            foreach (var claim in claimsDescripitor.Subject.Claims.ToList())
            {
                Console.WriteLine($"Type: {claim.GetType}, Value: {claim.Value}");
            }

            var tokenDescriptor = claimsDescripitor;

            var token = tokenHandler.CreateToken(tokenDescriptor);
  
            return tokenHandler.WriteToken(token);
        }


        [HttpPost("register")]
        [Route("/api/register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest registerModel)
        {
            if (await _context.Users.AnyAsync(u => u.Email == registerModel.Email))
            {
                return BadRequest("Email address is already in use.");
            }
            
            byte[] salt = GenerateSalt();
            string passwordHash = HashPassword(registerModel.Password, salt);


            User newUser = new User
            {
                FirstName = registerModel.FirstName,
                LastName = registerModel.LastName,
                Email = registerModel.Email,
                PasswordHash = passwordHash,
                Salt = salt,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok();
        }
        private static byte[] GenerateSalt()
        {
            byte[] salt = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }
        private string HashPassword(string password, byte[] salt)
        {
            using var hmac = new HMACSHA512(salt);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hash);
        }

        [Authorize]
        [HttpGet]
        [Route("/api/users/profile")]
        public IActionResult GetUserProfile()
        {
            var token = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            Console.WriteLine("Token: " + token);
            
            foreach (var claim in User.Claims)
            {
                Console.WriteLine($"Type: {claim.Type}, Value: {claim.Value}");
            }

            int userId;
            if (!int.TryParse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value, out userId))
            {
                return BadRequest("Invalid user ID in the token.");
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                return NotFound();
            }

            var userProfile = new
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                CreatedAt = user.CreatedAt
            };

            return Ok(userProfile);
        }

        [HttpPut("/api/orders/{id}/complete")]
        [Route("/api/orders/{id}/complete")]
        public async Task<IActionResult> CompleteOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            order.IsDelivered = true;
            _context.Entry(order).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrderExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return NoContent();
        }
        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }

    }
}
