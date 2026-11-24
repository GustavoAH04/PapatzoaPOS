using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using PapatzoaPOS.Modelos;

namespace PapatzoaPOS.Servicios
{
    // Cerebro del sistema: coordina inventario, clientes, proveedores, ventas y persistencia CSV
    public class GestorPapatzoa
    {
        // Contenedores asociativos (O(1)) indexados por código/ID
        private readonly Dictionary<string, Producto> productos = new Dictionary<string, Producto>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Cliente> clientes = new Dictionary<string, Cliente>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Proveedor> proveedores = new Dictionary<string, Proveedor>(StringComparer.OrdinalIgnoreCase);
        private readonly List<Venta> ventas = new List<Venta>();
        private readonly List<CompraProveedor> compras = new List<CompraProveedor>();

        // Rutas de almacenamiento local junto al ejecutable
        public string CarpetaDatos { get; }
        public string CarpetaTickets { get; }
        private string RutaProductos => Path.Combine(CarpetaDatos, "productos.csv");
        private string RutaClientes => Path.Combine(CarpetaDatos, "clientes.csv");
        private string RutaProveedores => Path.Combine(CarpetaDatos, "proveedores.csv");
        private string RutaVentas => Path.Combine(CarpetaDatos, "ventas.csv");
        private string RutaCompras => Path.Combine(CarpetaDatos, "compras.csv");

        public GestorPapatzoa()
        {
            CarpetaDatos = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DatosPapatzoa");
            CarpetaTickets = Path.Combine(CarpetaDatos, "Tickets");
            Directory.CreateDirectory(CarpetaDatos);
            Directory.CreateDirectory(CarpetaTickets);

            CargarTodosLosDatos();
        }

        // =====================================================================
        // CONSULTAS DE SOLO LECTURA PARA LA INTERFAZ
        // =====================================================================
        public List<Producto> ObtenerProductos() => productos.Values.OrderBy(p => p.Codigo).ToList();
        public List<Cliente> ObtenerClientes() => clientes.Values.OrderBy(c => c.Id).ToList();
        public List<Proveedor> ObtenerProveedores() => proveedores.Values.OrderBy(p => p.Id).ToList();
        public List<Venta> ObtenerVentas() => ventas.OrderByDescending(v => v.Fecha).ToList();
        public List<CompraProveedor> ObtenerCompras() => compras.OrderByDescending(c => c.Fecha).ToList();

        public List<Producto> FiltrarProductos(string categoria, string textoBusqueda)
        {
            IEnumerable<Producto> consulta = productos.Values;

            if (!string.IsNullOrWhiteSpace(categoria) && !categoria.Equals("Todos", StringComparison.OrdinalIgnoreCase))
            {
                consulta = consulta.Where(p => p.Categoria.Equals(categoria, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(textoBusqueda))
            {
                string filtro = textoBusqueda.Trim().ToLowerInvariant();
                consulta = consulta.Where(p =>
                    p.Nombre.ToLowerInvariant().Contains(filtro) ||
                    p.Codigo.ToLowerInvariant().Contains(filtro) ||
                    p.ObtenerDetallesEspecificos().ToLowerInvariant().Contains(filtro));
            }

            return consulta.OrderBy(p => p.Codigo).ToList();
        }

        public List<Producto> ObtenerProductosStockBajo()
        {
            return productos.Values.Where(p => p.TieneStockBajo()).OrderBy(p => p.Stock).ToList();
        }

        public Producto? BuscarProducto(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return null;
            return productos.TryGetValue(codigo.Trim(), out var prod) ? prod : null;
        }

        public Cliente? BuscarCliente(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return clientes.TryGetValue(id.Trim(), out var cli) ? cli : null;
        }

        public Proveedor? BuscarProveedor(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return proveedores.TryGetValue(id.Trim(), out var prov) ? prov : null;
        }

        // =====================================================================
        // GENERADORES DE FOLIOS E IDENTIFICADORES
        // =====================================================================
        public string GenerarFolioVenta() => $"VTA-{ventas.Count + 1:D4}";
        public string GenerarFolioCompra() => $"CMP-{compras.Count + 1:D4}";
        public string GenerarIdCliente() => $"CLI-{clientes.Count + 1:D3}";
        public string GenerarIdProveedor() => $"PRV-{proveedores.Count + 1:D3}";

        // =====================================================================
        // GESTIÓN DE PRODUCTOS, CLIENTES Y PROVEEDORES
        // =====================================================================
        public void AgregarProducto(Producto producto)
        {
            if (producto == null)
                throw new ArgumentNullException(nameof(producto));

            if (productos.ContainsKey(producto.Codigo))
                throw new InvalidOperationException($"Ya existe un producto con el código '{producto.Codigo}'.");

            productos[producto.Codigo] = producto;
            GuardarProductos();
        }

        public void ActualizarProducto(string codigo, string nuevoNombre, double nuevoPrecioBase, int nuevoStock, int nuevoStockMinimo)
        {
            var producto = BuscarProducto(codigo);
            if (producto == null)
                throw new KeyNotFoundException($"No se encontró el producto con código '{codigo}'.");

            producto.Nombre = nuevoNombre;
            producto.PrecioBase = nuevoPrecioBase;
            producto.Stock = nuevoStock;
            producto.StockMinimo = nuevoStockMinimo;
            GuardarProductos();
        }

        public bool EliminarProducto(string codigo)
        {
            if (productos.Remove(codigo.Trim()))
            {
                GuardarProductos();
                return true;
            }
            return false;
        }

        public void RegistrarCliente(Cliente cliente)
        {
            if (cliente == null)
                throw new ArgumentNullException(nameof(cliente));

            if (clientes.ContainsKey(cliente.Id))
                throw new InvalidOperationException($"Ya existe un cliente con el ID '{cliente.Id}'.");

            clientes[cliente.Id] = cliente;
            GuardarClientes();
        }

        public void RegistrarProveedor(Proveedor proveedor)
        {
            if (proveedor == null)
                throw new ArgumentNullException(nameof(proveedor));

            if (proveedores.ContainsKey(proveedor.Id))
                throw new InvalidOperationException($"Ya existe un proveedor con el ID '{proveedor.Id}'.");

            proveedores[proveedor.Id] = proveedor;
            GuardarProveedores();
        }

        // =====================================================================
        // PROCESAMIENTO DE VENTAS Y COMPRAS A PROVEEDORES
        // =====================================================================
        public string ProcesarVenta(Venta venta, double montoRecibido)
        {
            if (venta == null)
                throw new ArgumentNullException(nameof(venta));

            if (venta.Detalles.Count == 0)
                throw new InvalidOperationException("El carrito está vacío. Agrega al menos un artículo.");

            // 1. Validar existencias acumuladas por producto antes de modificar inventario
            var cantidadesPorCodigo = venta.Detalles
                .GroupBy(d => d.CodigoProducto, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Cantidad), StringComparer.OrdinalIgnoreCase);

            foreach (var par in cantidadesPorCodigo)
            {
                var prod = BuscarProducto(par.Key);
                if (prod == null)
                    throw new InvalidOperationException($"El producto '{par.Key}' ya no existe en el catálogo.");

                if (prod.Stock < par.Value)
                    throw new InvalidOperationException(
                        $"Stock insuficiente para '{prod.Nombre}'. Disponible: {prod.Stock}, solicitado: {par.Value}.");
            }

            // 2. Finalizar cobro (valida monto recibido y actualiza sellos del cliente)
            venta.FinalizarCobro(montoRecibido);

            // 3. Descontar stock real del inventario
            foreach (var par in cantidadesPorCodigo)
            {
                productos[par.Key].DescontarStock(par.Value);
            }

            // 4. Registrar venta, guardar ticket .txt y persistir todos los CSV
            ventas.Add(venta);
            string rutaTicket = venta.GuardarTicketArchivo(CarpetaTickets);

            GuardarProductos();
            GuardarClientes();
            GuardarVentas();

            return rutaTicket;
        }

        public CompraProveedor RegistrarCompraProveedor(string idProveedor, string codigoProducto, int cantidad, double costoUnitario)
        {
            var proveedor = BuscarProveedor(idProveedor)
                ?? throw new KeyNotFoundException("Selecciona un proveedor válido.");

            var producto = BuscarProducto(codigoProducto)
                ?? throw new KeyNotFoundException("Selecciona un producto válido para surtir.");

            string folio = GenerarFolioCompra();
            var compra = new CompraProveedor(folio, proveedor, producto, cantidad, costoUnitario);

            producto.AgregarStock(cantidad);
            compras.Add(compra);

            GuardarProductos();
            GuardarCompras();

            return compra;
        }

        // =====================================================================
        // CORTE DE CAJA Y RESUMEN DEL DÍA
        // =====================================================================
        public string ObtenerResumenCorteCajaHoy()
        {
            DateTime hoy = DateTime.Today;
            var ventasHoy = ventas.Where(v => v.Fecha.Date == hoy).ToList();
            var comprasHoy = compras.Where(c => c.Fecha.Date == hoy).ToList();

            double ingresosHoy = ventasHoy.Sum(v => v.Total);
            double descuentosComboHoy = ventasHoy.Sum(v => v.DescuentoCombo);
            double descuentosCortesiaHoy = ventasHoy.Sum(v => v.DescuentoCortesia);
            int combosAplicados = ventasHoy.Count(v => v.AplicaComboApapacho);
            double egresosProveedoresHoy = comprasHoy.Sum(c => c.TotalCompra);
            double balanceHoy = ingresosHoy - egresosProveedoresHoy;

            double ingresosHistoricos = ventas.Sum(v => v.Total);
            int alertasStock = ObtenerProductosStockBajo().Count;

            var sb = new StringBuilder();
            sb.AppendLine("================================================");
            sb.AppendLine("        CORTE DE CAJA — PAPATZOA POS            ");
            sb.AppendLine($"        Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}                  ");
            sb.AppendLine("================================================");
            sb.AppendLine($"Ventas realizadas hoy:         {ventasHoy.Count}");
            sb.AppendLine($"Combos Apapacho aplicados:     {combosAplicados}");
            sb.AppendLine($"Ingresos por ventas hoy:       ${ingresosHoy:F2}");
            sb.AppendLine($"Descuentos Combo (-10%):       ${descuentosComboHoy:F2}");
            sb.AppendLine($"Cortesías (5 Sellos):          ${descuentosCortesiaHoy:F2}");
            sb.AppendLine("------------------------------------------------");
            sb.AppendLine($"Compras a proveedores hoy:     {comprasHoy.Count}");
            sb.AppendLine($"Egresos a proveedores hoy:     ${egresosProveedoresHoy:F2}");
            sb.AppendLine($"Balance neto del día:          ${balanceHoy:F2}");
            sb.AppendLine("------------------------------------------------");
            sb.AppendLine($"Ventas acumuladas totales:     {ventas.Count} (${ingresosHistoricos:F2})");
            sb.AppendLine($"Productos con stock bajo:      {alertasStock}");
            sb.AppendLine("================================================");

            return sb.ToString();
        }

        // =====================================================================
        // CARGA Y GUARDADO DE ARCHIVOS CSV (PERSISTENCIA)
        // =====================================================================
        private void CargarTodosLosDatos()
        {
            if (File.Exists(RutaProductos))
                CargarProductos();
            else
            {
                PrecargarMenuPapatzoa();
                GuardarProductos();
            }

            if (File.Exists(RutaClientes))
                CargarClientes();
            else
            {
                PrecargarClientesIniciales();
                GuardarClientes();
            }

            if (File.Exists(RutaProveedores))
                CargarProveedores();
            else
            {
                PrecargarProveedoresIniciales();
                GuardarProveedores();
            }

            if (File.Exists(RutaVentas))
                CargarVentas();

            if (File.Exists(RutaCompras))
                CargarCompras();
        }

        private void PrecargarMenuPapatzoa()
        {
            // Bebidas artesanales tradicionales (sin jarabes artificiales)
            var iniciales = new List<Producto>
            {
                new BebidaArtesanal("BEB-01", "Café de Olla Tradicional", 38.00, 30, "Olla de barro con canela", "Mediano", "Piloncillo", 8),
                new BebidaArtesanal("BEB-02", "Café Americano de Altura", 35.00, 35, "Extracción artesanal Veracruz", "Mediano", "Natural", 8),
                new BebidaArtesanal("BEB-03", "Café en Prensa Francesa", 45.00, 20, "Prensa francesa", "Grande", "Azúcar morena", 6),
                new BebidaArtesanal("BEB-04", "Chocolate de Metate", 48.00, 25, "Batido tradicional con molinillo", "Mediano", "Piloncillo", 6),
                new BebidaArtesanal("BEB-05", "Té de Canela y Manzana", 32.00, 20, "Infusión natural", "Chico", "Miel de abeja", 5),

                // Panqués artesanales (especialidad de la casa)
                new PanqueArtesanal("PNQ-01", "Panqué de Nata Artesanal", 42.00, 18, "Nata de rancho", "Rebanada", true, false, 5),
                new PanqueArtesanal("PNQ-02", "Panqué de Elote Tierno", 40.00, 15, "Elote fresco", "Rebanada", true, false, 5),
                new PanqueArtesanal("PNQ-03", "Panqué de Nuez y Canela", 44.00, 14, "Nuez tostada y canela", "Rebanada", true, false, 5),
                new PanqueArtesanal("PNQ-04", "Panqué de Plátano Macho", 38.00, 12, "Plátano caramelizado", "Rebanada", true, false, 4),
                new PanqueArtesanal("PNQ-05", "Panqué de Zanahoria Casero", 45.00, 10, "Zanahoria y especias", "Rebanada", false, false, 4),
                new PanqueArtesanal("PNQ-06", "Panqué de Nata (Entero)", 240.00, 4, "Nata de rancho", "Panqué entero", false, false, 2),

                // Pan tradicional mexicano
                new PanTradicional("PAN-01", "Concha de Vainilla", 22.00, 24, "Brioche artesanal", true, 6),
                new PanTradicional("PAN-02", "Concha de Chocolate", 22.00, 20, "Brioche con cacao", true, 6),
                new PanTradicional("PAN-03", "Garibaldi Tradicional", 25.00, 16, "Panqué de mantequilla y gragea", true, 5),
                new PanTradicional("PAN-04", "Oreja de Hojaldre", 20.00, 18, "Hojaldre caramelizado", true, 5),
                new PanTradicional("PAN-05", "Polvorón de Nuez", 18.00, 22, "Masa quebrada con nuez", true, 6)
            };

            foreach (var prod in iniciales)
            {
                productos[prod.Codigo] = prod;
            }
        }

        private void PrecargarClientesIniciales()
        {
            var c1 = new Cliente("CLI-001", "Sofía Méndez", "464-112-3344", "sofia.mendez@correo.com", 4);
            var c2 = new Cliente("CLI-002", "Prof. Gibi (Cliente VIP)", "464-555-0199", "gibi.papatzoa@correo.com", 5);
            clientes[c1.Id] = c1;
            clientes[c2.Id] = c2;
        }

        private void PrecargarProveedoresIniciales()
        {
            var p1 = new Proveedor("PRV-001", "Don Mateo Cruz", "228-400-1122", "ventas@cafetalsierra.mx",
                                   "Cafetal Sierra de Veracruz", "CSV890412AB1", "Café en grano y piloncillo");
            var p2 = new Proveedor("PRV-002", "Elena Morales", "462-310-8877", "pedidos@molinoeltrigo.mx",
                                   "Molino El Trigo Dorado", "MTD950821CD4", "Harinas y levaduras");
            var p3 = new Proveedor("PRV-003", "Rancho San Isidro", "464-648-2200", "contacto@rancholanata.mx",
                                   "Lácteos y Nata La Tradición", "LNT010315EF9", "Nata fresca y mantequilla");
            proveedores[p1.Id] = p1;
            proveedores[p2.Id] = p2;
            proveedores[p3.Id] = p3;
        }

        private static string LimpiarCsv(string texto)
        {
            return (texto ?? string.Empty).Replace(";", ",").Replace("\r", " ").Replace("\n", " ").Trim();
        }

        private void GuardarProductos()
        {
            var lineas = new List<string>
            {
                "Tipo;Codigo;Nombre;PrecioBase;Stock;StockMinimo;Campo1;Campo2;Campo3;Campo4"
            };

            foreach (var p in ObtenerProductos())
            {
                string precio = p.PrecioBase.ToString("F2", CultureInfo.InvariantCulture);

                if (p is BebidaArtesanal b)
                {
                    lineas.Add($"Bebida;{LimpiarCsv(b.Codigo)};{LimpiarCsv(b.Nombre)};{precio};{b.Stock};{b.StockMinimo};{LimpiarCsv(b.MetodoPreparacion)};{LimpiarCsv(b.Tamano)};{LimpiarCsv(b.Endulzante)};");
                }
                else if (p is PanqueArtesanal pq)
                {
                    lineas.Add($"Panque;{LimpiarCsv(pq.Codigo)};{LimpiarCsv(pq.Nombre)};{precio};{pq.Stock};{pq.StockMinimo};{LimpiarCsv(pq.Sabor)};{LimpiarCsv(pq.Presentacion)};{pq.ServirTibio};{pq.ConExtraNata}");
                }
                else if (p is PanTradicional pt)
                {
                    lineas.Add($"Pan;{LimpiarCsv(pt.Codigo)};{LimpiarCsv(pt.Nombre)};{precio};{pt.Stock};{pt.StockMinimo};{LimpiarCsv(pt.TipoMasa)};{pt.RecienHorneado};;");
                }
            }

            File.WriteAllLines(RutaProductos, lineas, Encoding.UTF8);
        }

        private void CargarProductos()
        {
            productos.Clear();
            var lineas = File.ReadAllLines(RutaProductos, Encoding.UTF8);

            for (int i = 1; i < lineas.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lineas[i])) continue;
                var c = lineas[i].Split(';');
                if (c.Length < 8) continue;

                string tipo = c[0];
                string codigo = c[1];
                string nombre = c[2];
                double precio = double.Parse(c[3], CultureInfo.InvariantCulture);
                int stock = int.Parse(c[4], CultureInfo.InvariantCulture);
                int stockMin = int.Parse(c[5], CultureInfo.InvariantCulture);

                if (tipo == "Bebida" && c.Length >= 9)
                {
                    productos[codigo] = new BebidaArtesanal(codigo, nombre, precio, stock, c[6], c[7], c[8], stockMin);
                }
                else if (tipo == "Panque" && c.Length >= 10)
                {
                    bool tibio = bool.TryParse(c[8], out var t) && t;
                    bool nata = bool.TryParse(c[9], out var n) && n;
                    productos[codigo] = new PanqueArtesanal(codigo, nombre, precio, stock, c[6], c[7], tibio, nata, stockMin);
                }
                else if (tipo == "Pan")
                {
                    bool recien = bool.TryParse(c[7], out var r) && r;
                    productos[codigo] = new PanTradicional(codigo, nombre, precio, stock, c[6], recien, stockMin);
                }
            }
        }

        private void GuardarClientes()
        {
            var lineas = new List<string> { "Id;Nombre;Telefono;Correo;Sellos" };
            foreach (var c in ObtenerClientes())
            {
                lineas.Add($"{LimpiarCsv(c.Id)};{LimpiarCsv(c.Nombre)};{LimpiarCsv(c.Telefono)};{LimpiarCsv(c.Correo)};{c.SellosAcumulados}");
            }
            File.WriteAllLines(RutaClientes, lineas, Encoding.UTF8);
        }

        private void CargarClientes()
        {
            clientes.Clear();
            var lineas = File.ReadAllLines(RutaClientes, Encoding.UTF8);

            for (int i = 1; i < lineas.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lineas[i])) continue;
                var c = lineas[i].Split(';');
                if (c.Length < 5) continue;

                int sellos = int.TryParse(c[4], out var s) ? s : 0;
                clientes[c[0]] = new Cliente(c[0], c[1], c[2], c[3], sellos);
            }
        }

        private void GuardarProveedores()
        {
            var lineas = new List<string> { "Id;Contacto;Telefono;Correo;Empresa;RFC;CategoriaInsumo" };
            foreach (var p in ObtenerProveedores())
            {
                lineas.Add($"{LimpiarCsv(p.Id)};{LimpiarCsv(p.Nombre)};{LimpiarCsv(p.Telefono)};{LimpiarCsv(p.Correo)};{LimpiarCsv(p.Empresa)};{LimpiarCsv(p.Rfc)};{LimpiarCsv(p.CategoriaInsumo)}");
            }
            File.WriteAllLines(RutaProveedores, lineas, Encoding.UTF8);
        }

        private void CargarProveedores()
        {
            proveedores.Clear();
            var lineas = File.ReadAllLines(RutaProveedores, Encoding.UTF8);

            for (int i = 1; i < lineas.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lineas[i])) continue;
                var c = lineas[i].Split(';');
                if (c.Length < 7) continue;

                proveedores[c[0]] = new Proveedor(c[0], c[1], c[2], c[3], c[4], c[5], c[6]);
            }
        }

        private void GuardarVentas()
        {
            var lineas = new List<string>
            {
                "Folio;Fecha;Cliente;Subtotal;DescCombo;DescCortesia;Total;PagoRecibido;AplicoCombo;AplicoCortesia;Resumen"
            };

            foreach (var v in ventas)
            {
                lineas.Add(string.Join(";",
                    LimpiarCsv(v.Folio),
                    v.Fecha.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                    LimpiarCsv(v.NombreCliente),
                    v.Subtotal.ToString("F2", CultureInfo.InvariantCulture),
                    v.DescuentoCombo.ToString("F2", CultureInfo.InvariantCulture),
                    v.DescuentoCortesia.ToString("F2", CultureInfo.InvariantCulture),
                    v.Total.ToString("F2", CultureInfo.InvariantCulture),
                    v.PagoRecibido.ToString("F2", CultureInfo.InvariantCulture),
                    v.AplicaComboApapacho.ToString(),
                    v.AplicoCortesia.ToString(),
                    LimpiarCsv(v.ResumenArticulos)
                ));
            }

            File.WriteAllLines(RutaVentas, lineas, Encoding.UTF8);
        }

        private void CargarVentas()
        {
            ventas.Clear();
            var lineas = File.ReadAllLines(RutaVentas, Encoding.UTF8);

            for (int i = 1; i < lineas.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lineas[i])) continue;
                var c = lineas[i].Split(';');
                if (c.Length < 11) continue;

                DateTime fecha = DateTime.ParseExact(c[1], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                double subtotal = double.Parse(c[3], CultureInfo.InvariantCulture);
                double descCombo = double.Parse(c[4], CultureInfo.InvariantCulture);
                double descCortesia = double.Parse(c[5], CultureInfo.InvariantCulture);
                double total = double.Parse(c[6], CultureInfo.InvariantCulture);
                double pago = double.Parse(c[7], CultureInfo.InvariantCulture);
                bool combo = bool.TryParse(c[8], out var cb) && cb;
                bool cortesia = bool.TryParse(c[9], out var ct) && ct;

                ventas.Add(new Venta(c[0], fecha, c[2], subtotal, descCombo, descCortesia, total, pago, combo, cortesia, c[10]));
            }
        }

        private void GuardarCompras()
        {
            var lineas = new List<string>
            {
                "FolioCompra;Fecha;IdProveedor;EmpresaProveedor;CodigoProducto;NombreProducto;Cantidad;CostoUnitario"
            };

            foreach (var cp in compras)
            {
                lineas.Add(string.Join(";",
                    LimpiarCsv(cp.FolioCompra),
                    cp.Fecha.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                    LimpiarCsv(cp.IdProveedor),
                    LimpiarCsv(cp.EmpresaProveedor),
                    LimpiarCsv(cp.CodigoProducto),
                    LimpiarCsv(cp.NombreProducto),
                    cp.Cantidad.ToString(CultureInfo.InvariantCulture),
                    cp.CostoUnitario.ToString("F2", CultureInfo.InvariantCulture)
                ));
            }

            File.WriteAllLines(RutaCompras, lineas, Encoding.UTF8);
        }

        private void CargarCompras()
        {
            compras.Clear();
            var lineas = File.ReadAllLines(RutaCompras, Encoding.UTF8);

            for (int i = 1; i < lineas.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lineas[i])) continue;
                var c = lineas[i].Split(';');
                if (c.Length < 8) continue;

                DateTime fecha = DateTime.ParseExact(c[1], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                int cantidad = int.Parse(c[6], CultureInfo.InvariantCulture);
                double costo = double.Parse(c[7], CultureInfo.InvariantCulture);

                compras.Add(new CompraProveedor(c[0], fecha, c[2], c[3], c[4], c[5], cantidad, costo));
            }
        }
    }
}