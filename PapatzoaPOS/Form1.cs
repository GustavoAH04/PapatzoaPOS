using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using PapatzoaPOS.Modelos;
using PapatzoaPOS.Servicios;

namespace PapatzoaPOS
{
    public partial class Form1 : Form
    {
        // Motor del sistema y venta en curso
        private readonly GestorPapatzoa gestor;
        private Venta ventaActual;

        // Paleta de colores institucional de Papatzoa
        private readonly Color ColorCafeOscuro = Color.FromArgb(74, 44, 24);
        private readonly Color ColorCafeMedio = Color.FromArgb(110, 68, 40);
        private readonly Color ColorDorado = Color.FromArgb(184, 138, 68);
        private readonly Color ColorCremaFondo = Color.FromArgb(250, 246, 240);
        private readonly Color ColorTarjeta = Color.FromArgb(255, 253, 249);
        private readonly Color ColorVerdeCombo = Color.FromArgb(34, 118, 62);
        private readonly Color ColorAlerta = Color.FromArgb(185, 45, 35);

        // Estructura general de pestañas (las pestañas 2, 3 y 4 se completan en la Etapa 5)
        private TabControl tabPrincipal = null!;
        private TabPage tabPOS = null!;
        private TabPage tabInventario = null!;
        private TabPage tabPersonas = null!;
        private TabPage tabReportes = null!;
        private Label lblAlertaHeader = null!;

        // Controles de la Pestaña 1: Punto de Venta (Caja)
        private ComboBox cmbFiltroCategoria = null!;
        private TextBox txtBuscarPOS = null!;
        private DataGridView dgvCatalogoPOS = null!;
        private Panel pnlPersonalizacion = null!;
        private Label lblProductoSeleccionado = null!;
        private ComboBox cmbTamanoBebida = null!;
        private ComboBox cmbEndulzanteBebida = null!;
        private Label lblTamano = null!;
        private Label lblEndulzante = null!;
        private CheckBox chkServirTibio = null!;
        private CheckBox chkExtraNata = null!;
        private Label lblInfoPan = null!;
        private NumericUpDown numCantidadPOS = null!;
        private TextBox txtNotasPOS = null!;
        private Button btnAgregarCarrito = null!;

        // Controles del Carrito y Cobro
        private Label lblFolioVenta = null!;
        private ComboBox cmbClientePOS = null!;
        private Button btnAplicarCortesia = null!;
        private DataGridView dgvCarrito = null!;
        private Button btnQuitarArticulo = null!;
        private Button btnVaciarCarrito = null!;
        private Label lblEstadoCombo = null!;
        private Label lblSubtotal = null!;
        private Label lblDescuentos = null!;
        private Label lblTotalPagar = null!;
        private TextBox txtPagoRecibido = null!;
        private Label lblCambioCalculado = null!;
        private Button btnCobrar = null!;

        // Métodos parciales que se conectarán automáticamente en la Etapa 5
        partial void ConstruirModulosAdicionales();
        partial void RefrescarModulosAdicionales();

        private class OpcionCliente
        {
            public Cliente? Cliente { get; }
            public OpcionCliente(Cliente? cliente) { Cliente = cliente; }

            public override string ToString()
            {
                if (Cliente == null) return "Público General (Sin registro)";
                string insignia = Cliente.TieneCortesiaDisponible() ? " ★ CORTESÍA" : "";
                return $"{Cliente.Nombre} ({Cliente.SellosAcumulados}/5 sellos){insignia}";
            }
        }

        public Form1()
        {
            InitializeComponent();
            gestor = new GestorPapatzoa();
            ventaActual = new Venta(gestor.GenerarFolioVenta());

            ConstruirInterfazPrincipal();
            ConstruirModulosAdicionales();
            RefrescarTodo();
        }

        // =====================================================================
        // CONSTRUCCIÓN DE LA VENTANA PRINCIPAL Y ENCABEZADO PAPATZOA
        // =====================================================================
        private void ConstruirInterfazPrincipal()
        {
            Text = "PAPATZOA POS — Sabores que apapachan el alma";
            Size = new Size(1240, 760);
            MinimumSize = new Size(1060, 660);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = ColorCremaFondo;
            Font = new Font("Segoe UI", 9.5f);

            // Encabezado superior cálido
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 82,
                BackColor = ColorCafeOscuro,
                Padding = new Padding(16, 10, 16, 10)
            };

            var picLogo = new PictureBox
            {
                Size = new Size(64, 62),
                Location = new Point(16, 10),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White,
                Visible = false
            };

            int desplazamientoTexto = 18;
            if (CargarLogoSiExiste(picLogo))
            {
                picLogo.Visible = true;
                pnlHeader.Controls.Add(picLogo);
                desplazamientoTexto = 92;
            }

            var lblTitulo = new Label
            {
                Text = "PAPATZOA® — Cafetería y Panadería Artesanal",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(desplazamientoTexto, 12)
            };

            var lblEslogan = new Label
            {
                Text = "Sabores que apapachan el alma  •  Sistema de Punto de Venta (POO)",
                ForeColor = Color.FromArgb(236, 212, 168),
                Font = new Font("Segoe UI", 10f, FontStyle.Italic),
                AutoSize = true,
                Location = new Point(desplazamientoTexto + 2, 44)
            };

            lblAlertaHeader = new Label
            {
                Text = "Inventario saludable",
                ForeColor = Color.White,
                BackColor = ColorDorado,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(240, 34),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlHeader.Width - 260, 24)
            };

            pnlHeader.Controls.Add(lblTitulo);
            pnlHeader.Controls.Add(lblEslogan);
            pnlHeader.Controls.Add(lblAlertaHeader);

            // Contenedor de pestañas
            tabPrincipal = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Padding = new Point(14, 6)
            };

            tabPOS = new TabPage("☕ Punto de Venta (Caja)") { BackColor = ColorCremaFondo, Padding = new Padding(10) };
            tabInventario = new TabPage("🥖 Inventario y Menú") { BackColor = ColorCremaFondo, Padding = new Padding(10) };
            tabPersonas = new TabPage("👥 Clientes y Proveedores") { BackColor = ColorCremaFondo, Padding = new Padding(10) };
            tabReportes = new TabPage("📊 Historial y Corte de Caja") { BackColor = ColorCremaFondo, Padding = new Padding(10) };

            tabPrincipal.TabPages.AddRange(new[] { tabPOS, tabInventario, tabPersonas, tabReportes });

            Controls.Add(tabPrincipal);
            Controls.Add(pnlHeader);

            ConstruirTabPOS();
        }

        private bool CargarLogoSiExiste(PictureBox pic)
        {
            string[] rutas =
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo_papatzoa.png"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "logo_papatzoa.png")
            };

            foreach (var ruta in rutas)
            {
                if (File.Exists(ruta))
                {
                    try
                    {
                        using var fs = new FileStream(ruta, FileMode.Open, FileAccess.Read);
                        using var imgTemp = Image.FromStream(fs);
                        pic.Image = new Bitmap(imgTemp);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
            return false;
        }

        // =====================================================================
        // PESTAÑA 1: PUNTO DE VENTA (CATÁLOGO + PERSONALIZACIÓN + CARRITO)
        // =====================================================================
        private void ConstruirTabPOS()
        {
            var layoutPrincipal = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = ColorCremaFondo
            };
            layoutPrincipal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56f));
            layoutPrincipal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44f));

            // -----------------------------------------------------------------
            // COLUMNA IZQUIERDA: CATÁLOGO Y PERSONALIZACIÓN
            // -----------------------------------------------------------------
            var pnlIzquierdo = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 8, 0) };

            // Barra de filtros
            var pnlFiltros = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = ColorTarjeta,
                Padding = new Padding(8)
            };

            var lblCat = new Label { Text = "Categoría:", AutoSize = true, Location = new Point(8, 14), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = ColorCafeOscuro };
            cmbFiltroCategoria = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(82, 10),
                Width = 140,
                Font = new Font("Segoe UI", 9.5f)
            };
            cmbFiltroCategoria.Items.AddRange(new object[] { "Todos", "Bebida", "Panqué", "Pan Tradicional" });
            cmbFiltroCategoria.SelectedIndex = 0;
            cmbFiltroCategoria.SelectedIndexChanged += (s, e) => RefrescarCatalogoPOS();

            var lblBuscar = new Label { Text = "Buscar:", AutoSize = true, Location = new Point(236, 14), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = ColorCafeOscuro };
            txtBuscarPOS = new TextBox
            {
                Location = new Point(292, 10),
                Width = 220,
                Font = new Font("Segoe UI", 9.5f),
                PlaceholderText = "Ej. Café de olla, Nata, Concha..."
            };
            txtBuscarPOS.TextChanged += (s, e) => RefrescarCatalogoPOS();

            pnlFiltros.Controls.AddRange(new Control[] { lblCat, cmbFiltroCategoria, lblBuscar, txtBuscarPOS });

            // Panel inferior izquierdo: personalización polimórfica + agregar
            pnlPersonalizacion = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 165,
                BackColor = ColorTarjeta,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(10)
            };

            lblProductoSeleccionado = new Label
            {
                Text = "Selecciona un producto del catálogo para personalizarlo",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = ColorCafeOscuro,
                Location = new Point(10, 8),
                AutoSize = true
            };

            // Opciones dinámicas para BebidaArtesanal
            lblTamano = new Label { Text = "Tamaño:", Location = new Point(10, 42), AutoSize = true, Visible = false };
            cmbTamanoBebida = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(72, 38), Width = 145, Visible = false };
            cmbTamanoBebida.Items.AddRange(new object[] { "Chico (Base)", "Mediano (+$8)", "Grande (+$15)" });
            cmbTamanoBebida.SelectedIndex = 1;

            lblEndulzante = new Label { Text = "Endulzante:", Location = new Point(230, 42), AutoSize = true, Visible = false };
            cmbEndulzanteBebida = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(312, 38), Width = 155, Visible = false };
            cmbEndulzanteBebida.Items.AddRange(new object[] { "Piloncillo", "Azúcar morena", "Miel de abeja", "Natural (Sin azúcar)" });
            cmbEndulzanteBebida.SelectedIndex = 0;

            // Opciones dinámicas para PanqueArtesanal
            chkServirTibio = new CheckBox { Text = "Servir tibio recién salido", Location = new Point(12, 40), AutoSize = true, Checked = true, Visible = false };
            chkExtraNata = new CheckBox { Text = "Acompañar con extra nata artesanal (+$10.00)", Location = new Point(210, 40), AutoSize = true, Visible = false };

            // Etiqueta informativa para PanTradicional
            lblInfoPan = new Label
            {
                Text = "Pieza de panadería tradicional recién horneada (Precio fijo artesanal).",
                ForeColor = ColorCafeMedio,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                Location = new Point(10, 42),
                AutoSize = true,
                Visible = false
            };

            var lblCant = new Label { Text = "Cantidad:", Location = new Point(10, 82), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            numCantidadPOS = new NumericUpDown { Location = new Point(82, 78), Width = 68, Minimum = 1, Maximum = 99, Value = 1 };

            var lblNota = new Label { Text = "Nota:", Location = new Point(165, 82), AutoSize = true };
            txtNotasPOS = new TextBox { Location = new Point(210, 78), Width = 258, PlaceholderText = "Ej. Para llevar, poca canela..." };

            btnAgregarCarrito = CrearBoton("+ Agregar al Pedido", ColorDorado, Color.White, 220, 38);
            btnAgregarCarrito.Location = new Point(10, 116);
            btnAgregarCarrito.Click += (s, e) => AgregarProductoAlCarrito();

            pnlPersonalizacion.Controls.AddRange(new Control[]
            {
                lblProductoSeleccionado, lblTamano, cmbTamanoBebida, lblEndulzante, cmbEndulzanteBebida,
                chkServirTibio, chkExtraNata, lblInfoPan, lblCant, numCantidadPOS, lblNota, txtNotasPOS, btnAgregarCarrito
            });

            // Tabla central de catálogo
            dgvCatalogoPOS = CrearTablaEstilizada();
            dgvCatalogoPOS.Columns.Add("Codigo", "Código");
            dgvCatalogoPOS.Columns.Add("Categoria", "Categoría");
            dgvCatalogoPOS.Columns.Add("Nombre", "Producto");
            dgvCatalogoPOS.Columns.Add("Detalles", "Detalles Artesanales");
            dgvCatalogoPOS.Columns.Add("Precio", "Precio Base");
            dgvCatalogoPOS.Columns.Add("Stock", "Stock");
            dgvCatalogoPOS.Columns["Codigo"]!.FillWeight = 60;
            dgvCatalogoPOS.Columns["Categoria"]!.FillWeight = 85;
            dgvCatalogoPOS.Columns["Nombre"]!.FillWeight = 135;
            dgvCatalogoPOS.Columns["Detalles"]!.FillWeight = 150;
            dgvCatalogoPOS.Columns["Precio"]!.FillWeight = 75;
            dgvCatalogoPOS.Columns["Stock"]!.FillWeight = 55;
            dgvCatalogoPOS.SelectionChanged += (s, e) => ActualizarPanelPersonalizacion();
            dgvCatalogoPOS.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) AgregarProductoAlCarrito(); };

            pnlIzquierdo.Controls.Add(dgvCatalogoPOS);
            pnlIzquierdo.Controls.Add(pnlPersonalizacion);
            pnlIzquierdo.Controls.Add(pnlFiltros);

            // -----------------------------------------------------------------
            // COLUMNA DERECHA: CARRITO, COMBO APAPACHO Y COBRO
            // -----------------------------------------------------------------
            var pnlDerecho = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ColorTarjeta,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(12)
            };

            // Encabezado del carrito y selección de cliente
            var pnlTopCarrito = new Panel { Dock = DockStyle.Top, Height = 92 };
            lblFolioVenta = new Label
            {
                Text = $"Pedido actual: {ventaActual.Folio}",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = ColorCafeOscuro,
                Location = new Point(0, 2),
                AutoSize = true
            };

            var lblCli = new Label { Text = "Cliente:", Location = new Point(0, 34), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            cmbClientePOS = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(62, 30),
                Width = 285,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            cmbClientePOS.SelectedIndexChanged += (s, e) => CambiarClienteVentaActual();

            btnAplicarCortesia = CrearBoton("★ Canjear Cortesía (5 Sellos)", ColorCafeMedio, Color.White, 225, 28);
            btnAplicarCortesia.Location = new Point(0, 60);
            btnAplicarCortesia.Enabled = false;
            btnAplicarCortesia.Click += (s, e) => AplicarCortesiaCliente();

            pnlTopCarrito.Controls.AddRange(new Control[] { lblFolioVenta, lblCli, cmbClientePOS, btnAplicarCortesia });

            // Panel inferior derecho: totales, Combo Apapacho y cobro
            var pnlTotales = new Panel { Dock = DockStyle.Bottom, Height = 235, Padding = new Padding(0, 6, 0, 0) };

            btnQuitarArticulo = CrearBoton("Quitar seleccionado", Color.FromArgb(160, 75, 65), Color.White, 155, 30);
            btnQuitarArticulo.Location = new Point(0, 4);
            btnQuitarArticulo.Click += (s, e) => QuitarArticuloCarrito();

            btnVaciarCarrito = CrearBoton("Vaciar pedido", Color.FromArgb(120, 110, 102), Color.White, 125, 30);
            btnVaciarCarrito.Location = new Point(165, 4);
            btnVaciarCarrito.Click += (s, e) =>
            {
                ventaActual.VaciarCarrito();
                RefrescarCarrito();
            };

            lblEstadoCombo = new Label
            {
                Text = "Tip Papatzoa: Combina Café + Panqué/Pan para 10% de descuento",
                BackColor = Color.FromArgb(245, 236, 220),
                ForeColor = ColorCafeOscuro,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 42),
                Size = new Size(460, 28),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            lblSubtotal = new Label { Text = "Subtotal: $0.00", Location = new Point(0, 78), AutoSize = true, Font = new Font("Segoe UI", 10f) };
            lblDescuentos = new Label { Text = "Descuentos: -$0.00", Location = new Point(0, 100), AutoSize = true, Font = new Font("Segoe UI", 10f), ForeColor = ColorVerdeCombo };
            lblTotalPagar = new Label { Text = "TOTAL: $0.00", Location = new Point(0, 124), AutoSize = true, Font = new Font("Segoe UI", 15f, FontStyle.Bold), ForeColor = ColorCafeOscuro };

            var lblPago = new Label { Text = "Pago recibido ($):", Location = new Point(0, 164), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            txtPagoRecibido = new TextBox { Location = new Point(122, 160), Width = 110, Font = new Font("Segoe UI", 10.5f), PlaceholderText = "0.00" };
            txtPagoRecibido.TextChanged += (s, e) => ActualizarCambioEnVivo();

            lblCambioCalculado = new Label
            {
                Text = "Cambio: $0.00",
                Location = new Point(242, 163),
                AutoSize = true,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = ColorCafeMedio
            };

            btnCobrar = CrearBoton("✓ COBRAR Y GENERAR TICKET", ColorVerdeCombo, Color.White, 460, 40);
            btnCobrar.Location = new Point(0, 192);
            btnCobrar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            btnCobrar.Click += (s, e) => ProcesarCobroEnCaja();

            pnlTotales.Controls.AddRange(new Control[]
            {
                btnQuitarArticulo, btnVaciarCarrito, lblEstadoCombo,
                lblSubtotal, lblDescuentos, lblTotalPagar,
                lblPago, txtPagoRecibido, lblCambioCalculado, btnCobrar
            });

            // Tabla del carrito
            dgvCarrito = CrearTablaEstilizada();
            dgvCarrito.Columns.Add("Producto", "Artículo");
            dgvCarrito.Columns.Add("Detalle", "Preparación");
            dgvCarrito.Columns.Add("Cant", "Cant.");
            dgvCarrito.Columns.Add("Unitario", "Unit.");
            dgvCarrito.Columns.Add("Subtotal", "Subtotal");
            dgvCarrito.Columns["Producto"]!.FillWeight = 135;
            dgvCarrito.Columns["Detalle"]!.FillWeight = 140;
            dgvCarrito.Columns["Cant"]!.FillWeight = 45;
            dgvCarrito.Columns["Unitario"]!.FillWeight = 65;
            dgvCarrito.Columns["Subtotal"]!.FillWeight = 75;

            pnlDerecho.Controls.Add(dgvCarrito);
            pnlDerecho.Controls.Add(pnlTotales);
            pnlDerecho.Controls.Add(pnlTopCarrito);

            layoutPrincipal.Controls.Add(pnlIzquierdo, 0, 0);
            layoutPrincipal.Controls.Add(pnlDerecho, 1, 0);
            tabPOS.Controls.Add(layoutPrincipal);
        }

        // =====================================================================
        // LÓGICA INTERACTIVA DEL PUNTO DE VENTA
        // =====================================================================
        private void RefrescarTodo()
        {
            RefrescarCatalogoPOS();
            RefrescarClientesPOS();
            RefrescarCarrito();
            ActualizarBannerAlertas();
            RefrescarModulosAdicionales();
        }

        private void ActualizarBannerAlertas()
        {
            int bajos = gestor.ObtenerProductosStockBajo().Count;
            if (bajos > 0)
            {
                lblAlertaHeader.Text = $"⚠ {bajos} producto(s) con stock bajo";
                lblAlertaHeader.BackColor = ColorAlerta;
            }
            else
            {
                lblAlertaHeader.Text = "✓ Inventario al día";
                lblAlertaHeader.BackColor = ColorVerdeCombo;
            }
        }

        private void RefrescarCatalogoPOS()
        {
            string categoria = cmbFiltroCategoria.SelectedItem?.ToString() ?? "Todos";
            var lista = gestor.FiltrarProductos(categoria, txtBuscarPOS.Text);

            dgvCatalogoPOS.Rows.Clear();
            foreach (var p in lista)
            {
                int idx = dgvCatalogoPOS.Rows.Add(
                    p.Codigo,
                    p.Categoria,
                    p.Nombre,
                    p.ObtenerDetallesEspecificos(),
                    $"${p.PrecioBase:F2}",
                    p.Stock
                );

                if (p.Stock == 0)
                    dgvCatalogoPOS.Rows[idx].DefaultCellStyle.BackColor = Color.FromArgb(255, 224, 224);
                else if (p.TieneStockBajo())
                    dgvCatalogoPOS.Rows[idx].DefaultCellStyle.BackColor = Color.FromArgb(255, 243, 214);
            }

            ActualizarPanelPersonalizacion();
        }

        private void RefrescarClientesPOS()
        {
            string? idAnterior = ventaActual.ClienteAsociado?.Id;
            cmbClientePOS.Items.Clear();
            cmbClientePOS.Items.Add(new OpcionCliente(null));

            int indiceSeleccionar = 0;
            foreach (var c in gestor.ObtenerClientes())
            {
                int idx = cmbClientePOS.Items.Add(new OpcionCliente(c));
                if (idAnterior != null && c.Id.Equals(idAnterior, StringComparison.OrdinalIgnoreCase))
                    indiceSeleccionar = idx;
            }

            cmbClientePOS.SelectedIndex = indiceSeleccionar;
        }

        private void ActualizarPanelPersonalizacion()
        {
            lblTamano.Visible = cmbTamanoBebida.Visible = lblEndulzante.Visible = cmbEndulzanteBebida.Visible = false;
            chkServirTibio.Visible = chkExtraNata.Visible = false;
            lblInfoPan.Visible = false;

            if (dgvCatalogoPOS.CurrentRow == null)
            {
                lblProductoSeleccionado.Text = "Selecciona un producto del catálogo";
                return;
            }

            string? codigo = dgvCatalogoPOS.CurrentRow.Cells["Codigo"].Value?.ToString();
            var prod = codigo != null ? gestor.BuscarProducto(codigo) : null;
            if (prod == null) return;

            lblProductoSeleccionado.Text = $"{prod.Nombre}  —  Base: ${prod.PrecioBase:F2}  (Disponibles: {prod.Stock})";

            if (prod is BebidaArtesanal)
            {
                lblTamano.Visible = cmbTamanoBebida.Visible = lblEndulzante.Visible = cmbEndulzanteBebida.Visible = true;
            }
            else if (prod is PanqueArtesanal pq)
            {
                chkServirTibio.Checked = pq.ServirTibio;
                chkExtraNata.Checked = false;
                chkServirTibio.Visible = chkExtraNata.Visible = true;
            }
            else if (prod is PanTradicional)
            {
                lblInfoPan.Visible = true;
            }
        }

        private void AgregarProductoAlCarrito()
        {
            if (dgvCatalogoPOS.CurrentRow == null)
            {
                MessageBox.Show("Selecciona un producto del catálogo primero.", "Papatzoa POS", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string? codigo = dgvCatalogoPOS.CurrentRow.Cells["Codigo"].Value?.ToString();
            var prodBase = codigo != null ? gestor.BuscarProducto(codigo) : null;
            if (prodBase == null) return;

            int cantidad = (int)numCantidadPOS.Value;
            int yaEnCarrito = ventaActual.Detalles
                .Where(d => d.CodigoProducto.Equals(prodBase.Codigo, StringComparison.OrdinalIgnoreCase))
                .Sum(d => d.Cantidad);

            if (yaEnCarrito + cantidad > prodBase.Stock)
            {
                MessageBox.Show(
                    $"Stock insuficiente para '{prodBase.Nombre}'.\nDisponible: {prodBase.Stock} | Ya en pedido: {yaEnCarrito}",
                    "Inventario insuficiente", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Instancia personalizada para aplicar polimorfismo en CalcularPrecioFinal()
            Producto productoParaPedido = prodBase;
            if (prodBase is BebidaArtesanal beb)
            {
                string tamano = cmbTamanoBebida.SelectedIndex switch
                {
                    0 => "Chico",
                    2 => "Grande",
                    _ => "Mediano"
                };
                string endulzante = cmbEndulzanteBebida.SelectedItem?.ToString() ?? "Piloncillo";
                productoParaPedido = new BebidaArtesanal(
                    beb.Codigo, beb.Nombre, beb.PrecioBase, beb.Stock,
                    beb.MetodoPreparacion, tamano, endulzante, beb.StockMinimo);
            }
            else if (prodBase is PanqueArtesanal pq)
            {
                productoParaPedido = new PanqueArtesanal(
                    pq.Codigo, pq.Nombre, pq.PrecioBase, pq.Stock,
                    pq.Sabor, pq.Presentacion, chkServirTibio.Checked, chkExtraNata.Checked, pq.StockMinimo);
            }

            ventaActual.AgregarDetalle(new DetalleVenta(productoParaPedido, cantidad, txtNotasPOS.Text));
            numCantidadPOS.Value = 1;
            txtNotasPOS.Clear();
            RefrescarCarrito();
        }

        private void QuitarArticuloCarrito()
        {
            if (dgvCarrito.CurrentRow == null) return;
            ventaActual.EliminarDetalle(dgvCarrito.CurrentRow.Index);
            RefrescarCarrito();
        }

        private void CambiarClienteVentaActual()
        {
            if (cmbClientePOS.SelectedItem is OpcionCliente opcion)
            {
                ventaActual.AsignarCliente(opcion.Cliente);
                ActualizarTotalesCarrito();
            }
        }

        private void AplicarCortesiaCliente()
        {
            if (ventaActual.SolicitarCortesiaCliente())
            {
                MessageBox.Show(
                    "¡Cortesía de 5 Sellos aplicada! Se descontará el artículo de menor precio en este pedido.",
                    "Programa Sellos de Apapacho",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                ActualizarTotalesCarrito();
            }
            else
            {
                MessageBox.Show(
                    "Agrega al menos un artículo y selecciona un cliente con cinco o más sellos.",
                    "Cortesía no disponible",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        // Evento requerido por Form1.Designer.cs
        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void RefrescarCarrito()
        {
            lblFolioVenta.Text = $"Pedido actual: {ventaActual.Folio}";
            dgvCarrito.Rows.Clear();

            foreach (var det in ventaActual.Detalles)
            {
                string prep = string.IsNullOrWhiteSpace(det.NotasExtra)
                    ? det.DetallesProducto
                    : $"{det.DetallesProducto} | Nota: {det.NotasExtra}";

                dgvCarrito.Rows.Add(
                    det.NombreProducto,
                    prep,
                    det.Cantidad,
                    $"${det.PrecioUnitario:F2}",
                    $"${det.Subtotal:F2}"
                );
            }

            ActualizarTotalesCarrito();
        }

        private void ActualizarTotalesCarrito()
        {
            if (ventaActual.AplicaComboApapacho)
            {
                lblEstadoCombo.Text = "★ ¡COMBO APAPACHO ACTIVADO! (Café + Pan = -10% automático)";
                lblEstadoCombo.BackColor = ColorVerdeCombo;
                lblEstadoCombo.ForeColor = Color.White;
            }
            else
            {
                lblEstadoCombo.Text = "Tip Papatzoa: Combina Café + Panqué/Pan para 10% de descuento";
                lblEstadoCombo.BackColor = Color.FromArgb(245, 236, 220);
                lblEstadoCombo.ForeColor = ColorCafeOscuro;
            }

            double totalDescuentos = ventaActual.DescuentoCombo + ventaActual.DescuentoCortesia;
            lblSubtotal.Text = $"Subtotal: ${ventaActual.Subtotal:F2}";
            lblDescuentos.Text = $"Descuentos (Combo: ${ventaActual.DescuentoCombo:F2} | Cortesía: ${ventaActual.DescuentoCortesia:F2}): -${totalDescuentos:F2}";
            lblTotalPagar.Text = $"TOTAL: ${ventaActual.Total:F2}";

            btnAplicarCortesia.Enabled =
                ventaActual.ClienteAsociado != null &&
                ventaActual.ClienteAsociado.TieneCortesiaDisponible() &&
                ventaActual.Detalles.Count > 0 &&
                !ventaActual.AplicoCortesia;

            ActualizarCambioEnVivo();
        }

        private void ActualizarCambioEnVivo()
        {
            if (TryParseMoneda(txtPagoRecibido.Text, out double pago) && pago >= ventaActual.Total && ventaActual.Total > 0)
            {
                lblCambioCalculado.Text = $"Cambio: ${(pago - ventaActual.Total):F2}";
                lblCambioCalculado.ForeColor = ColorVerdeCombo;
            }
            else
            {
                lblCambioCalculado.Text = "Cambio: $0.00";
                lblCambioCalculado.ForeColor = ColorCafeMedio;
            }
        }

        private void ProcesarCobroEnCaja()
        {
            if (ventaActual.Detalles.Count == 0)
            {
                MessageBox.Show("Agrega al menos un producto al pedido antes de cobrar.", "Carrito vacío", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!TryParseMoneda(txtPagoRecibido.Text, out double pago))
            {
                MessageBox.Show("Ingresa un monto de pago válido (por ejemplo: 100 o 150.50).", "Pago inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPagoRecibido.Focus();
                return;
            }

            try
            {
                string rutaTicket = gestor.ProcesarVenta(ventaActual, pago);
                string textoTicket = ventaActual.GenerarTicketTexto();

                MostrarVentanaTicket(textoTicket, rutaTicket);

                ventaActual = new Venta(gestor.GenerarFolioVenta());
                txtPagoRecibido.Clear();
                RefrescarTodo();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "No se pudo completar la venta", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MostrarVentanaTicket(string contenidoTicket, string rutaArchivo)
        {
            using var frmTicket = new Form
            {
                Text = "Ticket de Venta — Papatzoa",
                Size = new Size(460, 560),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = ColorCremaFondo,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var txtTicket = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10f),
                BackColor = Color.White,
                Text = contenidoTicket
            };

            var pnlInferior = new Panel { Dock = DockStyle.Bottom, Height = 72, Padding = new Padding(10) };
            var lblRuta = new Label
            {
                Text = $"Guardado en: {Path.GetFileName(rutaArchivo)}",
                Location = new Point(10, 6),
                AutoSize = true,
                ForeColor = ColorCafeMedio,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic)
            };

            var btnCerrar = CrearBoton("Aceptar y Continuar", ColorCafeOscuro, Color.White, 180, 34);
            btnCerrar.Location = new Point(130, 30);
            btnCerrar.Click += (s, e) => frmTicket.Close();

            pnlInferior.Controls.AddRange(new Control[] { lblRuta, btnCerrar });
            frmTicket.Controls.Add(txtTicket);
            frmTicket.Controls.Add(pnlInferior);
            frmTicket.ShowDialog(this);
        }

        // =====================================================================
        // UTILIDADES VISUALES COMPARTIDAS ENTRE PESTAÑAS
        // =====================================================================
        private static bool TryParseMoneda(string texto, out double valor)
        {
            string limpio = (texto ?? string.Empty).Trim().Replace("$", "").Replace(",", ".");
            return double.TryParse(limpio, NumberStyles.Any, CultureInfo.InvariantCulture, out valor);
        }

        private Button CrearBoton(string texto, Color fondo, Color textoColor, int ancho, int alto)
        {
            var btn = new Button
            {
                Text = texto,
                BackColor = fondo,
                ForeColor = textoColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Size = new Size(ancho, alto),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private DataGridView CrearTablaEstilizada()
        {
            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                Font = new Font("Segoe UI", 9f)
            };

            dgv.ColumnHeadersDefaultCellStyle.BackColor = ColorCafeOscuro;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            dgv.ColumnHeadersHeight = 32;
            dgv.DefaultCellStyle.SelectionBackColor = ColorDorado;
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 247, 242);

            return dgv;
        }
    }
}