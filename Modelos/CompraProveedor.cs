using System;

namespace PapatzoaPOS.Modelos
{
    // Registra una compra de insumos o piezas a un proveedor para surtir inventario
    public class CompraProveedor
    {
        private string folioCompra = string.Empty;
        private string idProveedor = string.Empty;
        private string empresaProveedor = string.Empty;
        private string codigoProducto = string.Empty;
        private string nombreProducto = string.Empty;
        private int cantidad;
        private double costoUnitario;

        public DateTime Fecha { get; private set; }

        // Constructor para nueva compra desde la interfaz
        public CompraProveedor(string folioCompra, Proveedor proveedor, Producto producto,
                               int cantidad, double costoUnitario)
        {
            if (proveedor == null)
                throw new ArgumentNullException(nameof(proveedor));
            if (producto == null)
                throw new ArgumentNullException(nameof(producto));

            FolioCompra = folioCompra;
            Fecha = DateTime.Now;
            IdProveedor = proveedor.Id;
            EmpresaProveedor = proveedor.Empresa;
            CodigoProducto = producto.Codigo;
            NombreProducto = producto.Nombre;
            Cantidad = cantidad;
            CostoUnitario = costoUnitario;
        }

        // Constructor para cargar historial de compras desde CSV
        public CompraProveedor(string folioCompra, DateTime fecha, string idProveedor,
                               string empresaProveedor, string codigoProducto, string nombreProducto,
                               int cantidad, double costoUnitario)
        {
            FolioCompra = folioCompra;
            Fecha = fecha;
            IdProveedor = idProveedor;
            EmpresaProveedor = empresaProveedor;
            CodigoProducto = codigoProducto;
            NombreProducto = nombreProducto;
            Cantidad = cantidad;
            CostoUnitario = costoUnitario;
        }

        public string FolioCompra
        {
            get { return folioCompra; }
            private set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("El folio de compra es obligatorio.");
                folioCompra = value.Trim().ToUpper();
            }
        }

        public string IdProveedor
        {
            get { return idProveedor; }
            private set { idProveedor = value?.Trim() ?? string.Empty; }
        }

        public string EmpresaProveedor
        {
            get { return empresaProveedor; }
            private set { empresaProveedor = string.IsNullOrWhiteSpace(value) ? "Proveedor" : value.Trim(); }
        }

        public string CodigoProducto
        {
            get { return codigoProducto; }
            private set { codigoProducto = value?.Trim().ToUpper() ?? string.Empty; }
        }

        public string NombreProducto
        {
            get { return nombreProducto; }
            private set { nombreProducto = value?.Trim() ?? string.Empty; }
        }

        public int Cantidad
        {
            get { return cantidad; }
            private set
            {
                if (value <= 0)
                    throw new ArgumentException("La cantidad comprada debe ser mayor a cero.");
                cantidad = value;
            }
        }

        public double CostoUnitario
        {
            get { return costoUnitario; }
            private set
            {
                if (value <= 0)
                    throw new ArgumentException("El costo de compra debe ser mayor a cero.");
                costoUnitario = Math.Round(value, 2);
            }
        }

        public double TotalCompra => Math.Round(Cantidad * CostoUnitario, 2);

        public string ObtenerResumen()
        {
            return $"[{FolioCompra}] {Fecha:dd/MM/yyyy} | {EmpresaProveedor} | +{Cantidad} {NombreProducto} (${TotalCompra:F2})";
        }
    }
}