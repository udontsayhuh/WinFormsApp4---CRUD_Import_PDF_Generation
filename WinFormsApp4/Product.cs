using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinFormsApp4
{
    public class Product
    {
        public int ProductId { get; set; }

        public string? Name { get; set; }

        public int CategoryId { get; set; }
        public virtual Category Category { get; set; } = null!;
    }
}
