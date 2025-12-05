using DataEntities;

namespace Products.Models
{
    public static class DbInitializer
    {
        public static void Initialize(Context context)
        {
            if (context.Product.Any())
                return;

            // Updated product seed list: home remodel / DIY focused products
            // Replaced original outdoor/camping demo items with items like paint, drills, lumber, etc.
            var products = new List<Product>
        {
            new Product { Name = "Interior Wall Paint - White Matte", Description = "Premium interior latex paint with smooth matte finish, low VOC.", Price = 29.99m, ImageUrl = "paint_white_1.png" },
            new Product { Name = "Exterior Wood Stain - Cedar", Description = "Weather-resistant wood stain for decks and siding with UV protection.", Price = 34.99m, ImageUrl = "wood_stain_cedar.png" },
            new Product { Name = "Cordless Drill Kit", Description = "18V cordless drill with two batteries, charger, and 25-piece bit set.", Price = 79.99m, ImageUrl = "cordless_drill_18v.png" },
            new Product { Name = "Circular Saw - 7 1/4\"", Description = "Powerful circular saw for precise cuts in plywood and dimensional lumber.", Price = 119.99m, ImageUrl = "circular_saw_7_14.png" },
            new Product { Name = "Plywood Sheet - 3/4 inch", Description = "High-quality furniture-grade plywood sheet, 4x8 ft, versatile for cabinetry and shelving.", Price = 49.99m, ImageUrl = "plywood_3_4_4x8.png" },
            new Product { Name = "Pressure-Treated Lumber - 2x4", Description = "2x4 pressure-treated lumber, suitable for outdoor framing and decks.", Price = 6.49m, ImageUrl = "lumber_2x4.png" },
            new Product { Name = "Painter's Roller Kit", Description = "Complete roller kit with roller covers, tray, and extension pole for smooth wall coverage.", Price = 19.99m, ImageUrl = "painters_roller_kit.png" },
            new Product { Name = "Finish Nails - Box 1000", Description = "1 1/4 inch finish nails for trim and finishing work.", Price = 7.99m, ImageUrl = "finish_nails_box.png" },
            new Product { Name = "Wood Glue - 16 oz", Description = "High-strength PVA wood glue for furniture and cabinetry projects.", Price = 6.99m, ImageUrl = "wood_glue_16oz.png" },
            new Product { Name = "Sandpaper Assortment", Description = "Assorted grit sandpaper pack (80-400 grit) for rough and fine sanding.", Price = 9.99m, ImageUrl = "sandpaper_assortment.png" },
            new Product { Name = "Stud Finder", Description = "Electronic stud finder for locating studs, live wires, and edges behind walls.", Price = 24.99m, ImageUrl = "stud_finder.png" },
            new Product { Name = "Caulking Gun + Silicone", Description = "Smooth-action caulking gun with a tube of silicone sealant for gaps and joints.", Price = 12.99m, ImageUrl = "caulking_gun_silicone.png" },
            new Product { Name = "Toolbox - Metal", Description = "Durable metal toolbox with removable tray for organising hand tools.", Price = 39.99m, ImageUrl = "metal_toolbox.png" },
            new Product { Name = "Tape Measure - 25ft", Description = "25-foot tape measure with locking mechanism and belt clip.", Price = 9.49m, ImageUrl = "tape_measure_25ft.png" },
            new Product { Name = "Protective Safety Glasses", Description = "ANSI-rated safety glasses with anti-fog coating for eye protection.", Price = 6.49m, ImageUrl = "safety_glasses.png" },
        };

            context.AddRange(products);

            // sample add 500 products
            // context.AddRange(GetProductsToAdd(500, products));

            context.SaveChanges();
        }

        private static List<Product> GetProductsToAdd(int count, List<Product> baseProducts)
        {
            var productsToAdd = new List<Product>();
            for (int i = 1; i < count; i++)
            {
                foreach (var product in baseProducts)
                {
                    var newproduct = new Product
                    {
                        Name = $"{product.Name}-{i}",
                        Description = product.Description,
                        ImageUrl = product.ImageUrl,
                        Price = product.Price
                    };
                    productsToAdd.Add(newproduct);
                }
            }
            return productsToAdd;
        }
    }
}
