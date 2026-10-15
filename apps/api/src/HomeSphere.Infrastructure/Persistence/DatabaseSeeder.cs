using HomeSphere.Application.Common.Interfaces;
using HomeSphere.Domain.Entities;
using HomeSphere.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HomeSphere.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        HomeSphereDbContext context,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger logger)
    {
        // 1. Seed Roles
        var roleNames = new[]
        {
            (Role.Customer, "Registered customer with shopping permissions"),
            (Role.Admin, "System administrator with full access"),
            (Role.Warehouse, "Warehouse staff managing inventory and fulfillment"),
            (Role.Manager, "Store manager supervising orders, promotions and metrics")
        };

        foreach (var (name, description) in roleNames)
        {
            if (!await context.Roles.AnyAsync(r => r.Name == name))
            {
                context.Roles.Add(new Role
                {
                    Name = name,
                    Description = description
                });
                logger.LogInformation("Seeded role {RoleName}", name);
            }
        }

        await context.SaveChangesAsync();

        // 2. Seed Development Admin User
        var adminEmail = configuration["Seed:AdminEmail"] ?? "admin@homesphere.dev";
        var adminPassword = configuration["Seed:AdminPassword"] ?? "Admin123!";

        var existingAdmin = await context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Email == adminEmail.ToLowerInvariant());

        if (existingAdmin == null)
        {
            var adminRole = await context.Roles.FirstAsync(r => r.Name == Role.Admin);
            var managerRole = await context.Roles.FirstAsync(r => r.Name == Role.Manager);

            var adminUser = new User
            {
                Email = adminEmail.ToLowerInvariant(),
                PasswordHash = passwordHasher.HashPassword(adminPassword),
                FirstName = "Admin",
                LastName = "HomeSphere",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            adminUser.AddRole(adminRole);
            adminUser.AddRole(managerRole);

            context.Users.Add(adminUser);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded development admin user: {AdminEmail}", adminEmail);
        }

        // 3. Seed Catalog (Categories, Brands, Products, Variants, Images)
        if (!await context.Categories.AnyAsync())
        {
            logger.LogInformation("Seeding catalog data (categories, brands, products)...");

            // Categories
            var sala = new Category { Name = "Sala", Slug = "sala", Description = "Mobiliario y piezas de diseño para salas de estar modernas", DisplayOrder = 1 };
            var comedor = new Category { Name = "Comedor", Slug = "comedor", Description = "Espacios de encuentro, mesas y sillas con carácter", DisplayOrder = 2 };
            var dormitorio = new Category { Name = "Dormitorio", Slug = "dormitorio", Description = "Confort, descanso y calidez en acabados naturales", DisplayOrder = 3 };
            var iluminacion = new Category { Name = "Iluminación", Slug = "iluminacion", Description = "Lámparas colgantes, de pie y sobremesa contemporáneas", DisplayOrder = 4 };
            var decoracion = new Category { Name = "Decoración y Textiles", Slug = "decoracion-y-textiles", Description = "Detalles, alfombras de lana y cojines para elevar cada rincón", DisplayOrder = 5 };

            context.Categories.AddRange(sala, comedor, dormitorio, iluminacion, decoracion);
            await context.SaveChangesAsync();

            // Subcategories
            var sofas = new Category { Name = "Sofás y Sillones", Slug = "sofas-y-sillones", ParentId = sala.Id, DisplayOrder = 1 };
            var mesasCentro = new Category { Name = "Mesas de Centro", Slug = "mesas-de-centro", ParentId = sala.Id, DisplayOrder = 2 };
            var mueblesTv = new Category { Name = "Muebles de TV", Slug = "muebles-tv", ParentId = sala.Id, DisplayOrder = 3 };

            var mesasComedor = new Category { Name = "Mesas de Comedor", Slug = "mesas-comedor", ParentId = comedor.Id, DisplayOrder = 1 };
            var sillasComedor = new Category { Name = "Sillas de Comedor", Slug = "sillas-comedor", ParentId = comedor.Id, DisplayOrder = 2 };
            var aparadores = new Category { Name = "Aparadores", Slug = "aparadores", ParentId = comedor.Id, DisplayOrder = 3 };

            var camas = new Category { Name = "Camas y Cabeceros", Slug = "camas-y-cabeceros", ParentId = dormitorio.Id, DisplayOrder = 1 };
            var mesitasNoche = new Category { Name = "Mesitas de Noche", Slug = "mesitas-noche", ParentId = dormitorio.Id, DisplayOrder = 2 };

            var lamparasTecho = new Category { Name = "Lámparas de Techo", Slug = "lamparas-techo", ParentId = iluminacion.Id, DisplayOrder = 1 };
            var lamparasPie = new Category { Name = "Lámparas de Pie", Slug = "lamparas-pie", ParentId = iluminacion.Id, DisplayOrder = 2 };

            var alfombras = new Category { Name = "Alfombras", Slug = "alfombras", ParentId = decoracion.Id, DisplayOrder = 1 };
            var cojines = new Category { Name = "Cojines y Mantas", Slug = "cojines-mantas", ParentId = decoracion.Id, DisplayOrder = 2 };

            context.Categories.AddRange(
                sofas, mesasCentro, mueblesTv,
                mesasComedor, sillasComedor, aparadores,
                camas, mesitasNoche,
                lamparasTecho, lamparasPie,
                alfombras, cojines
            );
            await context.SaveChangesAsync();

            // Brands
            var nordicLiving = new Brand { Name = "Nordic Living", Slug = "nordic-living", Description = "Diseño escandinavo minimalista con maderas claras y líneas sobrias" };
            var casaModerna = new Brand { Name = "Casa Moderna", Slug = "casa-moderna", Description = "Estilo contemporáneo urbano con toques industriales y metales mate" };
            var atelierMadera = new Brand { Name = "Atelier Madera", Slug = "atelier-madera", Description = "Carpintería artesanal en maderas macizas certificadas (roble, nogal)" };
            var luxDesign = new Brand { Name = "LuxDesign", Slug = "luxdesign", Description = "Mobiliario premium de lujo silencioso, mármoles pulidos y tapizados bouclé" };
            var ecoHome = new Brand { Name = "EcoHome", Slug = "ecohome", Description = "Textiles orgánicos, fibras naturales sustentables y tintes ecológicos" };

            context.Brands.AddRange(nordicLiving, casaModerna, atelierMadera, luxDesign, ecoHome);
            await context.SaveChangesAsync();

            // Products
            var p1 = new Product
            {
                Name = "Sofá Modular Nordhavn 3 Cuerpos",
                Slug = "sofa-modular-nordhavn-3-cuerpos",
                ShortDescription = "Sofá modular de 3 cuerpos tapizado en lino texturizado con patas de roble macizo.",
                Description = "El sofá modular Nordhavn combina el minimalismo escandinavo con la máxima comodidad ergonómica. Su estructura interna de madera de haya seca y su relleno multicapa de alta resiliencia aseguran una durabilidad excepcional sin perder firmeza ni suavidad con el paso del tiempo.",
                BasePrice = 1450.00m,
                Status = ProductStatus.Active,
                IsFeatured = true,
                CategoryId = sofas.Id,
                BrandId = nordicLiving.Id,
                Variants = new List<ProductVariant>
                {
                    new() { Sku = "SOF-NORD-GRIS-3P", Name = "Gris Niebla / 3 Plazas (240 cm)", Price = 1450.00m, StockQuantity = 8, AttributesJson = "{\"color\":\"Gris Niebla\",\"material\":\"Lino Belga\",\"dimensions\":\"240 x 95 x 82 cm\"}" },
                    new() { Sku = "SOF-NORD-BEIGE-3P", Name = "Beige Arena / 3 Plazas (240 cm)", Price = 1450.00m, StockQuantity = 5, AttributesJson = "{\"color\":\"Beige Arena\",\"material\":\"Lino Belga\",\"dimensions\":\"240 x 95 x 82 cm\"}" },
                    new() { Sku = "SOF-NORD-TERR-3P", Name = "Terracota Cálido / 3 Plazas (240 cm)", Price = 1520.00m, StockQuantity = 3, AttributesJson = "{\"color\":\"Terracota Cálido\",\"material\":\"Lino Belga\",\"dimensions\":\"240 x 95 x 82 cm\"}" }
                },
                Images = new List<ProductImage>
                {
                    new() { Url = "https://images.unsplash.com/photo-1555041469-a586c61ea9bc?auto=format&fit=crop&w=1200&q=80", AltText = "Sofá Modular Nordhavn frontal", DisplayOrder = 1, IsPrimary = true },
                    new() { Url = "https://images.unsplash.com/photo-1493663284031-b7e3aefcae8e?auto=format&fit=crop&w=1200&q=80", AltText = "Sofá Nordhavn en sala de estar", DisplayOrder = 2, IsPrimary = false }
                }
            };

            var p2 = new Product
            {
                Name = "Mesa de Comedor Stockholm Roble Macizo",
                Slug = "mesa-comedor-stockholm-roble",
                ShortDescription = "Mesa de comedor extensible elaborada en roble europeo macizo con acabado al aceite natural.",
                Description = "Una pieza central majestuosa y atemporal. Stockholm destaca por sus bordes biselados sutiles y su ensamblaje tradicional. Su sistema de extensión oculta permite alojar cómodamente de 6 a 10 comensales.",
                BasePrice = 1190.00m,
                Status = ProductStatus.Active,
                IsFeatured = true,
                CategoryId = mesasComedor.Id,
                BrandId = atelierMadera.Id,
                Variants = new List<ProductVariant>
                {
                    new() { Sku = "MES-STCK-180-ROB", Name = "Roble Natural / 180x90 cm (Extensible a 240)", Price = 1190.00m, StockQuantity = 6, AttributesJson = "{\"material\":\"Roble Macizo\",\"finish\":\"Aceite Natural Mate\",\"dimensions\":\"180-240 x 90 x 75 cm\"}" },
                    new() { Sku = "MES-STCK-200-NOG", Name = "Nogal Americano / 200x100 cm (Extensible a 260)", Price = 1420.00m, StockQuantity = 4, AttributesJson = "{\"material\":\"Nogal Macizo\",\"finish\":\"Aceite Natural Oscuro\",\"dimensions\":\"200-260 x 100 x 75 cm\"}" }
                },
                Images = new List<ProductImage>
                {
                    new() { Url = "https://images.unsplash.com/photo-1615066390971-03e4e1c36ddf?auto=format&fit=crop&w=1200&q=80", AltText = "Mesa de comedor de madera maciza Stockholm", DisplayOrder = 1, IsPrimary = true }
                }
            };

            var p3 = new Product
            {
                Name = "Silla de Comedor Kanso Tapizada (Set x2)",
                Slug = "silla-comedor-kanso-set-x2",
                ShortDescription = "Juego de 2 sillas de comedor con estructura metálica negro mate y respaldo curvo ergonómico.",
                Description = "Equilibrio ideal entre ligereza visual y apoyo ergonómico. Tapizadas en tejido bouclé suave al tacto con tratamiento antimanchas de fácil limpieza.",
                BasePrice = 380.00m,
                Status = ProductStatus.Active,
                IsFeatured = false,
                CategoryId = sillasComedor.Id,
                BrandId = casaModerna.Id,
                Variants = new List<ProductVariant>
                {
                    new() { Sku = "SIL-KANS-BOUC-SET2", Name = "Bouclé Blanco Roto / Estructura Negra", Price = 380.00m, StockQuantity = 14, AttributesJson = "{\"upholstery\":\"Bouclé Premium\",\"structure\":\"Acero Mate\",\"quantity\":\"Set de 2\"}" },
                    new() { Sku = "SIL-KANS-VERD-SET2", Name = "Terciopelo Verde Oliva / Estructura Negra", Price = 395.00m, StockQuantity = 10, AttributesJson = "{\"upholstery\":\"Terciopelo\",\"structure\":\"Acero Mate\",\"quantity\":\"Set de 2\"}" }
                },
                Images = new List<ProductImage>
                {
                    new() { Url = "https://images.unsplash.com/photo-1503602642458-232111445657?auto=format&fit=crop&w=1200&q=80", AltText = "Sillas Kanso de comedor", DisplayOrder = 1, IsPrimary = true }
                }
            };

            var p4 = new Product
            {
                Name = "Lámpara Colgante Aurora Latón Cepillado",
                Slug = "lampara-colgante-aurora-laton",
                ShortDescription = "Lámpara colgante escultórica con difusor de vidrio opalino soplado y acentos en latón cepillado.",
                Description = "Diseñada para ser el punto focal de cualquier comedor o vestíbulo. Proporciona una iluminación difusa, cálida y envolvente que realza las texturas del espacio.",
                BasePrice = 260.00m,
                Status = ProductStatus.Active,
                IsFeatured = true,
                CategoryId = lamparasTecho.Id,
                BrandId = luxDesign.Id,
                Variants = new List<ProductVariant>
                {
                    new() { Sku = "LAM-AUR-LAT-35", Name = "Latón Dorado / Diámetro 35 cm", Price = 260.00m, StockQuantity = 12, AttributesJson = "{\"finish\":\"Latón Cepillado\",\"glass\":\"Opalino Soplado\",\"socket\":\"E27 LED Compatible\"}" },
                    new() { Sku = "LAM-AUR-NEG-35", Name = "Negro Satinado / Diámetro 35 cm", Price = 240.00m, StockQuantity = 8, AttributesJson = "{\"finish\":\"Negro Mate\",\"glass\":\"Opalino Soplado\",\"socket\":\"E27 LED Compatible\"}" }
                },
                Images = new List<ProductImage>
                {
                    new() { Url = "https://images.unsplash.com/photo-1513506003901-1e6a229e2d15?auto=format&fit=crop&w=1200&q=80", AltText = "Lámpara colgante Aurora", DisplayOrder = 1, IsPrimary = true }
                }
            };

            var p5 = new Product
            {
                Name = "Cama King Size Valldal con Cabecero Tapizado",
                Slug = "cama-king-valldal-cabecero",
                ShortDescription = "Estructura de cama King Size en fresno natural con cabecero tapizado desenfundable.",
                Description = "La cama Valldal ofrece una estética serena inspirada en los refugios nórdicos. Su base de láminas de madera amortiguadas garantiza un descanso reparador y silencioso.",
                BasePrice = 1320.00m,
                Status = ProductStatus.Active,
                IsFeatured = true,
                CategoryId = camas.Id,
                BrandId = nordicLiving.Id,
                Variants = new List<ProductVariant>
                {
                    new() { Sku = "CAM-VALL-KING-GRIS", Name = "King Size (180x200 cm) / Tapizado Gris Ceniza", Price = 1320.00m, StockQuantity = 4, AttributesJson = "{\"size\":\"King Size (180x200 cm)\",\"wood\":\"Fresno Natural\",\"upholstery\":\"Tejido Chenilla\"}" },
                    new() { Sku = "CAM-VALL-QUEEN-GRIS", Name = "Queen Size (160x200 cm) / Tapizado Gris Ceniza", Price = 1180.00m, StockQuantity = 7, AttributesJson = "{\"size\":\"Queen Size (160x200 cm)\",\"wood\":\"Fresno Natural\",\"upholstery\":\"Tejido Chenilla\"}" }
                },
                Images = new List<ProductImage>
                {
                    new() { Url = "https://images.unsplash.com/photo-1505693416388-ac5ce068fe85?auto=format&fit=crop&w=1200&q=80", AltText = "Cama King Size Valldal en dormitorio", DisplayOrder = 1, IsPrimary = true }
                }
            };

            var p6 = new Product
            {
                Name = "Alfombra de Lana Artesanal Atlas",
                Slug = "alfombra-lana-artesanal-atlas",
                ShortDescription = "Alfombra tejida a mano en 100% lana virgen no teñida con motivos geométricos sutiles.",
                Description = "Cada pieza de la colección Atlas es anudada a mano por artesanos expertos. Su pelo mullido aporta calidez térmica y acústica inigualable a cualquier habitación.",
                BasePrice = 410.00m,
                Status = ProductStatus.Active,
                IsFeatured = false,
                CategoryId = alfombras.Id,
                BrandId = ecoHome.Id,
                Variants = new List<ProductVariant>
                {
                    new() { Sku = "ALF-ATL-160X230", Name = "160 x 230 cm / Crudo y Grafito", Price = 410.00m, StockQuantity = 9, AttributesJson = "{\"material\":\"100% Lana Virgen\",\"weave\":\"Nudo a mano\",\"dimensions\":\"160 x 230 cm\"}" },
                    new() { Sku = "ALF-ATL-200X300", Name = "200 x 300 cm / Crudo y Grafito", Price = 620.00m, StockQuantity = 5, AttributesJson = "{\"material\":\"100% Lana Virgen\",\"weave\":\"Nudo a mano\",\"dimensions\":\"200 x 300 cm\"}" }
                },
                Images = new List<ProductImage>
                {
                    new() { Url = "https://images.unsplash.com/photo-1600121848594-d8644e57abab?auto=format&fit=crop&w=1200&q=80", AltText = "Alfombra de lana Atlas", DisplayOrder = 1, IsPrimary = true }
                }
            };

            var p7 = new Product
            {
                Name = "Mesa de Centro Nube Travertino",
                Slug = "mesa-centro-nube-travertino",
                ShortDescription = "Mesa de centro con silueta orgánica irregular esculpida en piedra travertino natural.",
                Description = "Inspirada en las formaciones calcáreas naturales, la mesa Nube cuenta con una superficie pulida mate con el poro abierto característico de la piedra travertino auténtica.",
                BasePrice = 890.00m,
                Status = ProductStatus.Active,
                IsFeatured = true,
                CategoryId = mesasCentro.Id,
                BrandId = luxDesign.Id,
                Variants = new List<ProductVariant>
                {
                    new() { Sku = "MES-TRAV-ORG-110", Name = "Travertino Beige Natural / 110 x 70 x 36 cm", Price = 890.00m, StockQuantity = 3, AttributesJson = "{\"material\":\"Piedra Travertino Natural\",\"finish\":\"Mate Poro Abierto\",\"dimensions\":\"110 x 70 x 36 cm\"}" }
                },
                Images = new List<ProductImage>
                {
                    new() { Url = "https://images.unsplash.com/photo-1533090161767-e6ffed986c88?auto=format&fit=crop&w=1200&q=80", AltText = "Mesa de centro en travertino orgánico", DisplayOrder = 1, IsPrimary = true }
                }
            };

            var p8 = new Product
            {
                Name = "Aparador 4 Puertas Ribbed Roble y Metal",
                Slug = "aparador-ribbed-roble-metal",
                ShortDescription = "Aparador contemporáneo con frentes alistonados en roble macizo y tiradores empotrados.",
                Description = "Gran capacidad de almacenamiento combinada con una presencia visual refinada. Sus frentes ranurados juegan con la luz y la sombra, complementando tanto salones como comedores.",
                BasePrice = 980.00m,
                Status = ProductStatus.Active,
                IsFeatured = false,
                CategoryId = aparadores.Id,
                BrandId = atelierMadera.Id,
                Variants = new List<ProductVariant>
                {
                    new() { Sku = "APA-RIB-180-ROB", Name = "Roble Natural Alistonado / 180 x 45 x 75 cm", Price = 980.00m, StockQuantity = 5, AttributesJson = "{\"material\":\"Roble Macizo y Enchapado\",\"doors\":\"4 puertas con freno suave\",\"dimensions\":\"180 x 45 x 75 cm\"}" }
                },
                Images = new List<ProductImage>
                {
                    new() { Url = "https://images.unsplash.com/photo-1595428774223-ef52624120d2?auto=format&fit=crop&w=1200&q=80", AltText = "Aparador alistonado de roble", DisplayOrder = 1, IsPrimary = true }
                }
            };

            context.Products.AddRange(p1, p2, p3, p4, p5, p6, p7, p8);
            await context.SaveChangesAsync();

            logger.LogInformation("Seeded 5 root categories, 12 subcategories, 5 brands, and 8 products with variants and images.");
        }
    }
}
