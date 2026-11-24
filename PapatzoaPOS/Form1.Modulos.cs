using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PapatzoaPOS.Modelos;

namespace PapatzoaPOS
{
    // Segunda parte de Form1: construye y controla Inventario, Personas y Reportes
    public partial class Form1
    {
        // =====================================================================
        // CONTROLES DE LA PESTAÑA 2: INVENTARIO Y COMPRAS A PROVEEDORES
        // =====================================================================
        private DataGridView dgvInventario = null!;
        private ComboBox cmbTipoProdInv = null!;
        private TextBox txtCodigoInv = null!;
        private TextBox txtNombreInv = null!;
        private TextBox txtPrecioInv = null!;
        private NumericUpDown numStockInv = null!;
        private NumericUpDown numStockMinInv = null!;
        private Label lblCampoExtra1 = null!;
        private TextBox txtCampoExtra1 = null!;
        private Label lblCampoExtra2 = null!;
        private TextBox txtCampoExtra2 = null!;

        private ComboBox cmbProveedorCompra = null!;
        private ComboBox cmbProductoCompra = null!;
        private NumericUpDown numCantidadCompra = null!;
        private TextBox txtCostoCompra = null!;

        // =====================================================================
        // CONTROLES DE LA PESTAÑA 3: CLIENTES Y PROVEEDORES
        // =====================================================================
        private DataGridView dgvClientes = null!;
        private TextBox txtCliNombre = null!;
        private TextBox txtCliTel = null!;
        private TextBox txtCliCorreo = null!;

        private DataGridView dgvProveedores = null!;
        private TextBox txtProvEmpresa = null!;
        private TextBox txtProvContacto = null!;
        private TextBox txtProvTel = null!;
        private TextBox txtProvCorreo = null!;
        private TextBox txtProvRfc = null!;
        private TextBox txtProvInsumo = null!;

        // =====================================================================
        // CONTROLES DE LA PESTAÑA 4: HISTORIAL Y CORTE DE CAJA
        // =====================================================================
        private DataGridView dgvHistorialVentas = null!;
        private DataGridView dgvHistorialCompras = null!;
        private TextBox txtCorteCaja = null!;

        // Clases auxiliares para mostrar nombres legibles en los ComboBox
        private class OpcionProveedor
        {
            public Proveedor Proveedor { get; }
            public OpcionProveedor(Proveedor proveedor) { Proveedor = proveedor; }
            public override string ToString() => $"{Proveedor.Empresa} ({Proveedor.Id})";
        }

        private class OpcionProducto
        {
            public Producto Producto { get; }
            public OpcionProducto(Producto producto) { Producto = producto; }
            public override string ToString() => $"{Producto.Codigo} — {Producto.Nombre} (Stock: {Producto.Stock})";
        }

        // Conecta automáticamente con el constructor de Form1.cs
        partial void ConstruirModulosAdicionales()
        {
            ConstruirTabInventario();
            ConstruirTabPersonas();
            ConstruirTabReportes();
        }

        // Conecta automáticamente con RefrescarTodo() en Form1.cs
        partial void RefrescarModulosAdicionales()
        {
            RefrescarTabInventario();
            RefrescarTabPersonas();
            RefrescarTabReportes();
        }

        // =====================================================================
        // PESTAÑA 2: INVENTARIO, ALTA/EDICIÓN Y COMPRA A PROVEEDORES
        // =====================================================================
        private void ConstruirTabInventario()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = ColorCremaFondo
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));

            // Izquierda: Tabla de Inventario
            var pnlTabla = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 10, 0) };
            var lblTituloTabla = new Label
            {
                Text = "Catálogo e Inventario Actual (clic en un renglón para editar precio o stock)",
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = ColorCafeOscuro
            };

            dgvInventario = CrearTablaEstilizada();
            dgvInventario.Columns.Add("Codigo", "Código");
            dgvInventario.Columns.Add("Categoria", "Categoría");
            dgvInventario.Columns.Add("Nombre", "Producto");
            dgvInventario.Columns.Add("Precio", "Precio Base");
            dgvInventario.Columns.Add("Stock", "Stock");
            dgvInventario.Columns.Add("Minimo", "Mín.");
            dgvInventario.Columns.Add("Estado", "Estado");
            dgvInventario.Columns["Codigo"]!.FillWeight = 65;
            dgvInventario.Columns["Categoria"]!.FillWeight = 85;
            dgvInventario.Columns["Nombre"]!.FillWeight = 150;
            dgvInventario.Columns["Precio"]!.FillWeight = 75;
            dgvInventario.Columns["Stock"]!.FillWeight = 50;
            dgvInventario.Columns["Minimo"]!.FillWeight = 45;
            dgvInventario.Columns["Estado"]!.FillWeight = 85;
            dgvInventario.SelectionChanged += (s, e) => CargarProductoSeleccionadoEnFormulario();

            pnlTabla.Controls.Add(dgvInventario);
            pnlTabla.Controls.Add(lblTituloTabla);

            // Derecha: Panel deslizable con 2 cajas (Gestión de Producto + Surtir con Proveedor)
            var pnlDerecho = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(2) };

            // --- Grupo 1: Alta y Edición de Productos ---
            var grpProducto = new GroupBox
            {
                Text = "  Alta y Edición de Productos  ",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = ColorCafeOscuro,
                BackColor = ColorTarjeta,
                Location = new Point(0, 0),
                Size = new Size(445, 315),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var lblTipo = new Label { Text = "Categoría:", Location = new Point(14, 30), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            cmbTipoProdInv = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(110, 26), Width = 145, Font = new Font("Segoe UI", 9f) };
            cmbTipoProdInv.Items.AddRange(new object[] { "Bebida", "Panqué", "Pan Tradicional" });
            cmbTipoProdInv.SelectedIndex = 0;
            cmbTipoProdInv.SelectedIndexChanged += (s, e) => ActualizarEtiquetasExtraInventario();

            var lblCod = new Label { Text = "Código:", Location = new Point(268, 30), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            txtCodigoInv = new TextBox { Location = new Point(324, 26), Width = 105, Font = new Font("Segoe UI", 9f), PlaceholderText = "Ej. PNQ-07" };

            var lblNom = new Label { Text = "Nombre:", Location = new Point(14, 62), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            txtNombreInv = new TextBox { Location = new Point(110, 58), Width = 319, Font = new Font("Segoe UI", 9f), PlaceholderText = "Ej. Panqué de Elote (Entero)" };

            var lblPre = new Label { Text = "Precio Base ($):", Location = new Point(14, 94), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            txtPrecioInv = new TextBox { Location = new Point(110, 90), Width = 90, Font = new Font("Segoe UI", 9f), PlaceholderText = "0.00" };

            var lblStk = new Label { Text = "Stock:", Location = new Point(212, 94), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            numStockInv = new NumericUpDown { Location = new Point(256, 90), Width = 62, Minimum = 0, Maximum = 999, Value = 10, Font = new Font("Segoe UI", 9f) };

            var lblMin = new Label { Text = "Mín.:", Location = new Point(328, 94), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            numStockMinInv = new NumericUpDown { Location = new Point(367, 90), Width = 62, Minimum = 0, Maximum = 100, Value = 5, Font = new Font("Segoe UI", 9f) };

            lblCampoExtra1 = new Label { Text = "Preparación:", Location = new Point(14, 128), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            txtCampoExtra1 = new TextBox { Location = new Point(110, 124), Width = 319, Font = new Font("Segoe UI", 9f), PlaceholderText = "Ej. Olla de barro / Sabor naranja" };

            lblCampoExtra2 = new Label { Text = "Endulzante:", Location = new Point(14, 160), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            txtCampoExtra2 = new TextBox { Location = new Point(110, 156), Width = 319, Font = new Font("Segoe UI", 9f), PlaceholderText = "Ej. Piloncillo / Rebanada o Panqué entero" };

            var btnNuevoProd = CrearBoton("+ Guardar Nuevo", ColorVerdeCombo, Color.White, 132, 34);
            btnNuevoProd.Location = new Point(14, 204);
            btnNuevoProd.Click += (s, e) => GuardarNuevoProducto();

            var btnActualizarProd = CrearBoton("Actualizar Precio/Stock", ColorDorado, Color.White, 168, 34);
            btnActualizarProd.Location = new Point(154, 204);
            btnActualizarProd.Click += (s, e) => ActualizarProductoExistente();

            var btnLimpiarProd = CrearBoton("Limpiar", Color.FromArgb(120, 110, 102), Color.White, 98, 34);
            btnLimpiarProd.Location = new Point(330, 204);
            btnLimpiarProd.Click += (s, e) => LimpiarFormularioInventario();

            var btnEliminarProd = CrearBoton("Eliminar Producto Seleccionado", ColorAlerta, Color.White, 414, 32);
            btnEliminarProd.Location = new Point(14, 252);
            btnEliminarProd.Click += (s, e) => EliminarProductoSeleccionado();

            grpProducto.Controls.AddRange(new Control[]
            {
                lblTipo, cmbTipoProdInv, lblCod, txtCodigoInv, lblNom, txtNombreInv,
                lblPre, txtPrecioInv, lblStk, numStockInv, lblMin, numStockMinInv,
                lblCampoExtra1, txtCampoExtra1, lblCampoExtra2, txtCampoExtra2,
                btnNuevoProd, btnActualizarProd, btnLimpiarProd, btnEliminarProd
            });

            // --- Grupo 2: Compra a Proveedores (Reabastecer Stock) ---
            var grpCompra = new GroupBox
            {
                Text = "  Comprar a Proveedor (Surtir Inventario)  ",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = ColorCafeOscuro,
                BackColor = ColorTarjeta,
                Location = new Point(0, 326),
                Size = new Size(445, 222),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var lblProvC = new Label { Text = "Proveedor:", Location = new Point(14, 32), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            cmbProveedorCompra = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(98, 28), Width = 330, Font = new Font("Segoe UI", 9f) };

            var lblProdC = new Label { Text = "Producto:", Location = new Point(14, 68), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            cmbProductoCompra = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(98, 64), Width = 330, Font = new Font("Segoe UI", 9f) };

            var lblCantC = new Label { Text = "Cantidad (+):", Location = new Point(14, 106), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            numCantidadCompra = new NumericUpDown { Location = new Point(98, 102), Width = 80, Minimum = 1, Maximum = 500, Value = 10, Font = new Font("Segoe UI", 9f) };

            var lblCostoC = new Label { Text = "Costo Unit. ($):", Location = new Point(195, 106), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            txtCostoCompra = new TextBox { Location = new Point(296, 102), Width = 132, Font = new Font("Segoe UI", 9f), PlaceholderText = "Ej. 18.50" };

            var btnRegistrarCompra = CrearBoton("✓ Registrar Compra y Sumar Stock", ColorCafeOscuro, Color.White, 414, 38);
            btnRegistrarCompra.Location = new Point(14, 152);
            btnRegistrarCompra.Click += (s, e) => ProcesarCompraProveedor();

            grpCompra.Controls.AddRange(new Control[]
            {
                lblProvC, cmbProveedorCompra, lblProdC, cmbProductoCompra,
                lblCantC, numCantidadCompra, lblCostoC, txtCostoCompra, btnRegistrarCompra
            });

            pnlDerecho.Controls.Add(grpProducto);
            pnlDerecho.Controls.Add(grpCompra);

            layout.Controls.Add(pnlTabla, 0, 0);
            layout.Controls.Add(pnlDerecho, 1, 0);
            tabInventario.Controls.Add(layout);

            ActualizarEtiquetasExtraInventario();
        }

        private void ActualizarEtiquetasExtraInventario()
        {
            string tipo = cmbTipoProdInv.SelectedItem?.ToString() ?? "Bebida";
            if (tipo == "Bebida")
            {
                lblCampoExtra1.Text = "Preparación:";
                txtCampoExtra1.PlaceholderText = "Ej. Olla de barro, Prensa francesa";
                lblCampoExtra2.Visible = txtCampoExtra2.Visible = true;
                lblCampoExtra2.Text = "Endulzante:";
                txtCampoExtra2.PlaceholderText = "Ej. Piloncillo, Azúcar morena, Natural";
            }
            else if (tipo == "Panqué")
            {
                lblCampoExtra1.Text = "Sabor:";
                txtCampoExtra1.PlaceholderText = "Ej. Nata, Naranja, Chocolate abuelita";
                lblCampoExtra2.Visible = txtCampoExtra2.Visible = true;
                lblCampoExtra2.Text = "Presentación:";
                txtCampoExtra2.PlaceholderText = "Ej. Rebanada o Panqué entero";
            }
            else
            {
                lblCampoExtra1.Text = "Tipo de masa:";
                txtCampoExtra1.PlaceholderText = "Ej. Brioche artesanal, Hojaldre";
                lblCampoExtra2.Visible = txtCampoExtra2.Visible = false;
            }
        }

        private void RefrescarTabInventario()
        {
            dgvInventario.Rows.Clear();
            cmbProductoCompra.Items.Clear();
            cmbProveedorCompra.Items.Clear();

            foreach (var p in gestor.ObtenerProductos())
            {
                string estado = p.Stock == 0 ? "AGOTADO" : (p.TieneStockBajo() ? "STOCK BAJO" : "OK");
                int idx = dgvInventario.Rows.Add(
                    p.Codigo, p.Categoria, p.Nombre, $"${p.PrecioBase:F2}", p.Stock, p.StockMinimo, estado);

                if (p.Stock == 0)
                    dgvInventario.Rows[idx].DefaultCellStyle.BackColor = Color.FromArgb(255, 224, 224);
                else if (p.TieneStockBajo())
                    dgvInventario.Rows[idx].DefaultCellStyle.BackColor = Color.FromArgb(255, 243, 214);

                cmbProductoCompra.Items.Add(new OpcionProducto(p));
            }

            foreach (var prov in gestor.ObtenerProveedores())
            {
                cmbProveedorCompra.Items.Add(new OpcionProveedor(prov));
            }

            if (cmbProductoCompra.Items.Count > 0) cmbProductoCompra.SelectedIndex = 0;
            if (cmbProveedorCompra.Items.Count > 0) cmbProveedorCompra.SelectedIndex = 0;
        }

        private void CargarProductoSeleccionadoEnFormulario()
        {
            if (dgvInventario.CurrentRow == null) return;
            string? codigo = dgvInventario.CurrentRow.Cells["Codigo"].Value?.ToString();
            var p = codigo != null ? gestor.BuscarProducto(codigo) : null;
            if (p == null) return;

            cmbTipoProdInv.SelectedItem = p.Categoria;
            txtCodigoInv.Text = p.Codigo;
            txtNombreInv.Text = p.Nombre;
            txtPrecioInv.Text = p.PrecioBase.ToString("F2");
            numStockInv.Value = Math.Min(numStockInv.Maximum, p.Stock);
            numStockMinInv.Value = Math.Min(numStockMinInv.Maximum, p.StockMinimo);

            if (p is BebidaArtesanal b)
            {
                txtCampoExtra1.Text = b.MetodoPreparacion;
                txtCampoExtra2.Text = b.Endulzante;
            }
            else if (p is PanqueArtesanal pq)
            {
                txtCampoExtra1.Text = pq.Sabor;
                txtCampoExtra2.Text = pq.Presentacion;
            }
            else if (p is PanTradicional pt)
            {
                txtCampoExtra1.Text = pt.TipoMasa;
                txtCampoExtra2.Clear();
            }
        }

        private void LimpiarFormularioInventario()
        {
            txtCodigoInv.Clear();
            txtNombreInv.Clear();
            txtPrecioInv.Clear();
            numStockInv.Value = 10;
            numStockMinInv.Value = 5;
            txtCampoExtra1.Clear();
            txtCampoExtra2.Clear();
            txtCodigoInv.Focus();
        }

        private void GuardarNuevoProducto()
        {
            if (!TryParseMoneda(txtPrecioInv.Text, out double precio))
            {
                MessageBox.Show("Ingresa un precio base válido.", "Dato inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string tipo = cmbTipoProdInv.SelectedItem?.ToString() ?? "Bebida";
                int stock = (int)numStockInv.Value;
                int stockMin = (int)numStockMinInv.Value;
                Producto nuevo;

                if (tipo == "Bebida")
                {
                    nuevo = new BebidaArtesanal(txtCodigoInv.Text, txtNombreInv.Text, precio, stock,
                        txtCampoExtra1.Text, "Mediano", txtCampoExtra2.Text, stockMin);
                }
                else if (tipo == "Panqué")
                {
                    string pres = string.IsNullOrWhiteSpace(txtCampoExtra2.Text) ? "Rebanada" : txtCampoExtra2.Text;
                    nuevo = new PanqueArtesanal(txtCodigoInv.Text, txtNombreInv.Text, precio, stock,
                        txtCampoExtra1.Text, pres, true, false, stockMin);
                }
                else
                {
                    nuevo = new PanTradicional(txtCodigoInv.Text, txtNombreInv.Text, precio, stock,
                        txtCampoExtra1.Text, true, stockMin);
                }

                gestor.AgregarProducto(nuevo);
                MessageBox.Show($"Producto '{nuevo.Nombre}' agregado al catálogo de Papatzoa.", "Producto registrado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LimpiarFormularioInventario();
                RefrescarTodo();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error al guardar producto", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ActualizarProductoExistente()
        {
            if (!TryParseMoneda(txtPrecioInv.Text, out double precio))
            {
                MessageBox.Show("Ingresa un precio válido para actualizar.", "Dato inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                gestor.ActualizarProducto(txtCodigoInv.Text, txtNombreInv.Text, precio, (int)numStockInv.Value, (int)numStockMinInv.Value);
                MessageBox.Show("Producto actualizado y guardado en inventario.", "Actualización exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefrescarTodo();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "No se pudo actualizar", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EliminarProductoSeleccionado()
        {
            if (dgvInventario.CurrentRow == null) return;
            string? codigo = dgvInventario.CurrentRow.Cells["Codigo"].Value?.ToString();
            string? nombre = dgvInventario.CurrentRow.Cells["Nombre"].Value?.ToString();
            if (string.IsNullOrWhiteSpace(codigo)) return;

            var resp = MessageBox.Show($"¿Seguro que deseas eliminar '{nombre}' ({codigo}) del menú?",
                "Confirmar eliminación", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (resp == DialogResult.Yes && gestor.EliminarProducto(codigo))
            {
                LimpiarFormularioInventario();
                RefrescarTodo();
            }
        }

        private void ProcesarCompraProveedor()
        {
            if (cmbProveedorCompra.SelectedItem is not OpcionProveedor opProv ||
                cmbProductoCompra.SelectedItem is not OpcionProducto opProd)
            {
                MessageBox.Show("Selecciona un proveedor y un producto para registrar la compra.", "Datos faltantes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!TryParseMoneda(txtCostoCompra.Text, out double costo))
            {
                MessageBox.Show("Ingresa un costo unitario válido (ej. 18.50).", "Costo inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var compra = gestor.RegistrarCompraProveedor(
                    opProv.Proveedor.Id, opProd.Producto.Codigo, (int)numCantidadCompra.Value, costo);

                MessageBox.Show(
                    $"Compra {compra.FolioCompra} registrada.\nSe sumaron +{compra.Cantidad} unidades a '{compra.NombreProducto}'.\nTotal invertido: ${compra.TotalCompra:F2}",
                    "Inventario Reabastecido", MessageBoxButtons.OK, MessageBoxIcon.Information);

                txtCostoCompra.Clear();
                numCantidadCompra.Value = 10;
                RefrescarTodo();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error en compra", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // =====================================================================
        // PESTAÑA 3: CLIENTES (SELLOS DE APAPACHO) Y PROVEEDORES
        // =====================================================================
        private void ConstruirTabPersonas()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = ColorCremaFondo
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            // --- COLUMNA IZQUIERDA: CLIENTES ---
            var pnlClientes = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 8, 0) };
            var grpCli = new GroupBox
            {
                Text = "  Registrar Cliente Frecuente (Programa Sellos de Apapacho)  ",
                Dock = DockStyle.Top,
                Height = 165,
                BackColor = ColorTarjeta,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = ColorCafeOscuro
            };

            var lblCN = new Label { Text = "Nombre:", Location = new Point(14, 30), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            txtCliNombre = new TextBox { Location = new Point(88, 26), Width = 240, Font = new Font("Segoe UI", 9f), PlaceholderText = "Nombre completo del cliente" };

            var lblCT = new Label { Text = "Teléfono:", Location = new Point(14, 62), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            txtCliTel = new TextBox { Location = new Point(88, 58), Width = 240, Font = new Font("Segoe UI", 9f), PlaceholderText = "Ej. 464-123-4567" };

            var lblCC = new Label { Text = "Correo:", Location = new Point(14, 94), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            txtCliCorreo = new TextBox { Location = new Point(88, 90), Width = 240, Font = new Font("Segoe UI", 9f), PlaceholderText = "cliente@correo.com" };

            var btnAddCli = CrearBoton("+ Registrar Cliente", ColorVerdeCombo, Color.White, 170, 34);
            btnAddCli.Location = new Point(345, 52);
            btnAddCli.Click += (s, e) => RegistrarNuevoCliente();

            var lblTipSellos = new Label
            {
                Text = "Cada compra suma 1 sello. Al reunir 5 sellos gana 1 pieza de cortesía en caja.",
                Location = new Point(14, 128),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = ColorCafeMedio
            };

            grpCli.Controls.AddRange(new Control[] { lblCN, txtCliNombre, lblCT, txtCliTel, lblCC, txtCliCorreo, btnAddCli, lblTipSellos });

            dgvClientes = CrearTablaEstilizada();
            dgvClientes.Columns.Add("Id", "ID");
            dgvClientes.Columns.Add("Nombre", "Cliente");
            dgvClientes.Columns.Add("Telefono", "Teléfono");
            dgvClientes.Columns.Add("Correo", "Correo");
            dgvClientes.Columns.Add("Sellos", "Sellos");
            dgvClientes.Columns.Add("Cortesia", "Beneficio");
            dgvClientes.Columns["Id"]!.FillWeight = 60;
            dgvClientes.Columns["Nombre"]!.FillWeight = 135;
            dgvClientes.Columns["Telefono"]!.FillWeight = 95;
            dgvClientes.Columns["Correo"]!.FillWeight = 120;
            dgvClientes.Columns["Sellos"]!.FillWeight = 55;
            dgvClientes.Columns["Cortesia"]!.FillWeight = 95;

            pnlClientes.Controls.Add(dgvClientes);
            pnlClientes.Controls.Add(grpCli);

            // --- COLUMNA DERECHA: PROVEEDORES ---
            var pnlProveedores = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 0, 0) };
            var grpProv = new GroupBox
            {
                Text = "  Registrar Proveedor Artesanal  ",
                Dock = DockStyle.Top,
                Height = 165,
                BackColor = ColorTarjeta,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = ColorCafeOscuro
            };

            var lblPE = new Label { Text = "Empresa:", Location = new Point(14, 28), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            txtProvEmpresa = new TextBox { Location = new Point(82, 24), Width = 185, Font = new Font("Segoe UI", 9f), PlaceholderText = "Ej. Molino Santa Fe" };

            var lblPC = new Label { Text = "Contacto:", Location = new Point(278, 28), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            txtProvContacto = new TextBox { Location = new Point(346, 24), Width = 175, Font = new Font("Segoe UI", 9f), PlaceholderText = "Nombre del vendedor" };

            var lblPT = new Label { Text = "Teléfono:", Location = new Point(14, 60), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            txtProvTel = new TextBox { Location = new Point(82, 56), Width = 185, Font = new Font("Segoe UI", 9f) };

            var lblPR = new Label { Text = "RFC:", Location = new Point(278, 60), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            txtProvRfc = new TextBox { Location = new Point(346, 56), Width = 175, Font = new Font("Segoe UI", 9f), PlaceholderText = "Opcional" };

            var lblPM = new Label { Text = "Correo:", Location = new Point(14, 92), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            txtProvCorreo = new TextBox { Location = new Point(82, 88), Width = 185, Font = new Font("Segoe UI", 9f) };

            var lblPI = new Label { Text = "Insumo:", Location = new Point(278, 92), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            txtProvInsumo = new TextBox { Location = new Point(346, 88), Width = 175, Font = new Font("Segoe UI", 9f), PlaceholderText = "Ej. Café, Harina, Nata" };

            var btnAddProv = CrearBoton("+ Registrar Proveedor", ColorDorado, Color.White, 220, 32);
            btnAddProv.Location = new Point(14, 122);
            btnAddProv.Click += (s, e) => RegistrarNuevoProveedor();

            grpProv.Controls.AddRange(new Control[]
            {
                lblPE, txtProvEmpresa, lblPC, txtProvContacto,
                lblPT, txtProvTel, lblPR, txtProvRfc,
                lblPM, txtProvCorreo, lblPI, txtProvInsumo, btnAddProv
            });

            dgvProveedores = CrearTablaEstilizada();
            dgvProveedores.Columns.Add("Id", "ID");
            dgvProveedores.Columns.Add("Empresa", "Empresa");
            dgvProveedores.Columns.Add("Contacto", "Contacto");
            dgvProveedores.Columns.Add("Insumo", "Insumo");
            dgvProveedores.Columns.Add("Telefono", "Teléfono");
            dgvProveedores.Columns.Add("RFC", "RFC");
            dgvProveedores.Columns["Id"]!.FillWeight = 55;
            dgvProveedores.Columns["Empresa"]!.FillWeight = 135;
            dgvProveedores.Columns["Contacto"]!.FillWeight = 105;
            dgvProveedores.Columns["Insumo"]!.FillWeight = 120;
            dgvProveedores.Columns["Telefono"]!.FillWeight = 85;
            dgvProveedores.Columns["RFC"]!.FillWeight = 85;

            pnlProveedores.Controls.Add(dgvProveedores);
            pnlProveedores.Controls.Add(grpProv);

            layout.Controls.Add(pnlClientes, 0, 0);
            layout.Controls.Add(pnlProveedores, 1, 0);
            tabPersonas.Controls.Add(layout);
        }

        private void RefrescarTabPersonas()
        {
            dgvClientes.Rows.Clear();
            foreach (var c in gestor.ObtenerClientes())
            {
                string beneficio = c.TieneCortesiaDisponible() ? "★ CORTESÍA LISTA" : $"Faltan {5 - c.SellosAcumulados}";
                int idx = dgvClientes.Rows.Add(c.Id, c.Nombre, c.Telefono, c.Correo, $"{c.SellosAcumulados}/5", beneficio);
                if (c.TieneCortesiaDisponible())
                    dgvClientes.Rows[idx].DefaultCellStyle.BackColor = Color.FromArgb(228, 247, 232);
            }

            dgvProveedores.Rows.Clear();
            foreach (var p in gestor.ObtenerProveedores())
            {
                dgvProveedores.Rows.Add(p.Id, p.Empresa, p.Nombre, p.CategoriaInsumo, p.Telefono, p.Rfc);
            }
        }

        private void RegistrarNuevoCliente()
        {
            try
            {
                var nuevo = new Cliente(gestor.GenerarIdCliente(), txtCliNombre.Text, txtCliTel.Text, txtCliCorreo.Text);
                gestor.RegistrarCliente(nuevo);

                MessageBox.Show($"Cliente '{nuevo.Nombre}' registrado con ID {nuevo.Id}.", "Registro exitoso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtCliNombre.Clear();
                txtCliTel.Clear();
                txtCliCorreo.Clear();
                RefrescarTodo();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error al registrar cliente", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RegistrarNuevoProveedor()
        {
            try
            {
                var nuevo = new Proveedor(
                    gestor.GenerarIdProveedor(),
                    txtProvContacto.Text,
                    txtProvTel.Text,
                    txtProvCorreo.Text,
                    txtProvEmpresa.Text,
                    txtProvRfc.Text,
                    txtProvInsumo.Text);

                gestor.RegistrarProveedor(nuevo);

                MessageBox.Show($"Proveedor '{nuevo.Empresa}' registrado con ID {nuevo.Id}.", "Registro exitoso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtProvEmpresa.Clear();
                txtProvContacto.Clear();
                txtProvTel.Clear();
                txtProvCorreo.Clear();
                txtProvRfc.Clear();
                txtProvInsumo.Clear();
                RefrescarTodo();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error al registrar proveedor", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // =====================================================================
        // PESTAÑA 4: HISTORIAL DE VENTAS, COMPRAS Y CORTE DE CAJA
        // =====================================================================
        private void ConstruirTabReportes()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = ColorCremaFondo
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38f));

            // Izquierda: 2 tablas apiladas (Ventas arriba, Compras abajo)
            var layoutIzq = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(0, 0, 10, 0)
            };
            layoutIzq.RowStyles.Add(new RowStyle(SizeType.Percent, 55f));
            layoutIzq.RowStyles.Add(new RowStyle(SizeType.Percent, 45f));

            // Panel Ventas
            var pnlVentas = new Panel { Dock = DockStyle.Fill };
            var pnlBarVentas = new Panel { Dock = DockStyle.Top, Height = 34 };
            var lblV = new Label { Text = "Historial de Ventas (doble clic para reimprimir ticket)", Location = new Point(0, 7), AutoSize = true, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = ColorCafeOscuro };
            var btnVerTicket = CrearBoton("Ver Ticket", ColorDorado, Color.White, 105, 28);
            btnVerTicket.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnVerTicket.Location = new Point(pnlBarVentas.Width - 108, 2);
            btnVerTicket.Click += (s, e) => VerTicketHistorialSeleccionado();
            pnlBarVentas.Controls.AddRange(new Control[] { lblV, btnVerTicket });

            dgvHistorialVentas = CrearTablaEstilizada();
            dgvHistorialVentas.Columns.Add("Folio", "Folio");
            dgvHistorialVentas.Columns.Add("Fecha", "Fecha y Hora");
            dgvHistorialVentas.Columns.Add("Cliente", "Cliente");
            dgvHistorialVentas.Columns.Add("Articulos", "Artículos");
            dgvHistorialVentas.Columns.Add("Combo", "Combo -10%");
            dgvHistorialVentas.Columns.Add("Total", "Total");
            dgvHistorialVentas.Columns["Folio"]!.FillWeight = 65;
            dgvHistorialVentas.Columns["Fecha"]!.FillWeight = 105;
            dgvHistorialVentas.Columns["Cliente"]!.FillWeight = 100;
            dgvHistorialVentas.Columns["Articulos"]!.FillWeight = 180;
            dgvHistorialVentas.Columns["Combo"]!.FillWeight = 65;
            dgvHistorialVentas.Columns["Total"]!.FillWeight = 70;
            dgvHistorialVentas.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) VerTicketHistorialSeleccionado(); };

            pnlVentas.Controls.Add(dgvHistorialVentas);
            pnlVentas.Controls.Add(pnlBarVentas);

            // Panel Compras
            var pnlCompras = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 0) };
            var lblC = new Label { Text = "Historial de Compras a Proveedores", Dock = DockStyle.Top, Height = 26, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = ColorCafeOscuro };
            dgvHistorialCompras = CrearTablaEstilizada();
            dgvHistorialCompras.Columns.Add("Folio", "Folio");
            dgvHistorialCompras.Columns.Add("Fecha", "Fecha");
            dgvHistorialCompras.Columns.Add("Proveedor", "Proveedor");
            dgvHistorialCompras.Columns.Add("Producto", "Producto");
            dgvHistorialCompras.Columns.Add("Cant", "Cant.");
            dgvHistorialCompras.Columns.Add("Total", "Total");
            dgvHistorialCompras.Columns["Folio"]!.FillWeight = 65;
            dgvHistorialCompras.Columns["Fecha"]!.FillWeight = 100;
            dgvHistorialCompras.Columns["Proveedor"]!.FillWeight = 135;
            dgvHistorialCompras.Columns["Producto"]!.FillWeight = 135;
            dgvHistorialCompras.Columns["Cant"]!.FillWeight = 50;
            dgvHistorialCompras.Columns["Total"]!.FillWeight = 75;

            pnlCompras.Controls.Add(dgvHistorialCompras);
            pnlCompras.Controls.Add(lblC);

            layoutIzq.Controls.Add(pnlVentas, 0, 0);
            layoutIzq.Controls.Add(pnlCompras, 0, 1);

            // Derecha: Corte de Caja y Exportación
            var pnlCorte = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ColorTarjeta,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(12)
            };

            var lblTituloCorte = new Label
            {
                Text = "Resumen y Corte de Caja Diario",
                Dock = DockStyle.Top,
                Height = 32,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = ColorCafeOscuro
            };

            var pnlBotonesCorte = new Panel { Dock = DockStyle.Bottom, Height = 96, Padding = new Padding(0, 8, 0, 0) };

            var btnExportarCorte = CrearBoton("Exportar Corte de Caja (.txt)", ColorVerdeCombo, Color.White, 400, 38);
            btnExportarCorte.Location = new Point(0, 8);
            btnExportarCorte.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            btnExportarCorte.Click += (s, e) => ExportarCorteCajaArchivo();

            var btnAbrirCarpeta = CrearBoton("Abrir Carpeta de CSV y Tickets", ColorCafeOscuro, Color.White, 400, 36);
            btnAbrirCarpeta.Location = new Point(0, 54);
            btnAbrirCarpeta.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            btnAbrirCarpeta.Click += (s, e) => AbrirCarpetaDatos();

            pnlBotonesCorte.Controls.AddRange(new Control[] { btnExportarCorte, btnAbrirCarpeta });

            txtCorteCaja = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10f),
                BackColor = Color.White,
                ForeColor = ColorCafeOscuro
            };

            pnlCorte.Controls.Add(txtCorteCaja);
            pnlCorte.Controls.Add(pnlBotonesCorte);
            pnlCorte.Controls.Add(lblTituloCorte);

            layout.Controls.Add(layoutIzq, 0, 0);
            layout.Controls.Add(pnlCorte, 1, 0);
            tabReportes.Controls.Add(layout);
        }

        private void RefrescarTabReportes()
        {
            dgvHistorialVentas.Rows.Clear();
            foreach (var v in gestor.ObtenerVentas())
            {
                dgvHistorialVentas.Rows.Add(
                    v.Folio,
                    v.Fecha.ToString("dd/MM/yyyy HH:mm"),
                    v.NombreCliente,
                    v.ResumenArticulos,
                    v.AplicaComboApapacho ? "Sí (-10%)" : "No",
                    $"${v.Total:F2}");
            }

            dgvHistorialCompras.Rows.Clear();
            foreach (var c in gestor.ObtenerCompras())
            {
                dgvHistorialCompras.Rows.Add(
                    c.FolioCompra,
                    c.Fecha.ToString("dd/MM/yyyy HH:mm"),
                    c.EmpresaProveedor,
                    c.NombreProducto,
                    c.Cantidad,
                    $"${c.TotalCompra:F2}");
            }

            txtCorteCaja.Text = gestor.ObtenerResumenCorteCajaHoy();
        }

        private void VerTicketHistorialSeleccionado()
        {
            if (dgvHistorialVentas.CurrentRow == null) return;
            string? folio = dgvHistorialVentas.CurrentRow.Cells["Folio"].Value?.ToString();
            var venta = gestor.ObtenerVentas().FirstOrDefault(v => v.Folio.Equals(folio, StringComparison.OrdinalIgnoreCase));
            if (venta == null) return;

            MostrarVentanaTicket(venta.GenerarTicketTexto(), "ventas.csv (Historial guardado)");
        }

        private void ExportarCorteCajaArchivo()
        {
            try
            {
                string nombreArchivo = $"CorteCaja_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string ruta = Path.Combine(gestor.CarpetaDatos, nombreArchivo);
                File.WriteAllText(ruta, gestor.ObtenerResumenCorteCajaHoy(), Encoding.UTF8);

                MessageBox.Show($"Corte de caja exportado correctamente:\n{ruta}", "Corte Guardado", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error al exportar", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AbrirCarpetaDatos()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = gestor.CarpetaDatos,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo abrir la carpeta:\n{gestor.CarpetaDatos}\n\n{ex.Message}", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}