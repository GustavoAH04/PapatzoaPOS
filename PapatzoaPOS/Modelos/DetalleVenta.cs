using System;

namespace PapatzoaPOS.Modelos
{
    // Representa un artículo individual dentro del carrito de compra
    public class DetalleVenta
    {
        private string codigoProducto = string.Empty;
        private string nombreProducto = string.Empty;
        private string categoria = string.Empty;
        private string detallesProducto = string.Empty;
        private double precioUnitario;
        private int cantidad;
        private string notasExtra = string.Empty;

        // Constructor principal: toma los datos de un Producto en tiempo real (usa polimorfismo)
        public DetalleVenta(Producto producto, int cantidad, string notasExtra = "")
        {
            if (producto == null)
                throw new ArgumentNullException(nameof(producto), "El producto no puede ser nulo.");

            CodigoProducto = producto.Codigo;
            NombreProducto = producto.Nombre;
            Categoria = producto.Categoria;
            DetallesProducto = producto.ObtenerDetallesEspecificos();
            PrecioUnitario = producto.CalcularPrecioFinal();
            Cantidad = cantidad;
            NotasExtra = notasExtra;
        }

        // Constructor alterno útil para reconstruir detalles desde archivos
        public DetalleVenta(string codigoProducto, string nombreProducto, string categoria,
                            string detallesProducto, double precioUnitario, int cantidad, string notasExtra = "")
        {
            CodigoProducto = codigoProducto;
            NombreProducto = nombreProducto;
            Categoria = categoria;
            DetallesProducto = detallesProducto;
            PrecioUnitario = precioUnitario;
            Cantidad = cantidad;
            NotasExtra = notasExtra;
        }

        public string CodigoProducto
        {
            get { return codigoProducto; }
            private set { codigoProducto = string.IsNullOrWhiteSpace(value) ? "S/C" : value.Trim(); }
        }

        public string NombreProducto
        {
            get { return nombreProducto; }
            private set { nombreProducto = string.IsNullOrWhiteSpace(value) ? "Producto" : value.Trim(); }
        }

        public string Categoria
        {
            get { return categoria; }
            private set { categoria = string.IsNullOrWhiteSpace(value) ? "General" : value.Trim(); }
        }

        public string DetallesProducto
        {
            get { return detallesProducto; }
            private set { detallesProducto = value?.Trim() ?? string.Empty; }
        }

        public double PrecioUnitario
        {
            get { return precioUnitario; }
            private set
            {
                if (value <= 0)
                    throw new ArgumentException("El precio unitario debe ser mayor a cero.");
                precioUnitario = Math.Round(value, 2);
            }
        }

        public int Cantidad
        {
            get { return cantidad; }
            set
            {
                if (value <= 0)
                    throw new ArgumentException("La cantidad debe ser al menos 1.");
                cantidad = value;
            }
        }

        public string NotasExtra
        {
            get { return notasExtra; }
            set { notasExtra = value?.Trim() ?? string.Empty; }
        }

        public double Subtotal => Math.Round(PrecioUnitario * Cantidad, 2);

        public string ObtenerLineaTicket()
        {
            string nota = string.IsNullOrWhiteSpace(NotasExtra) ? "" : $"\n    Nota: {NotasExtra}";
            return $"{Cantidad}x {NombreProducto} (${PrecioUnitario:F2} c/u) -> ${Subtotal:F2}\n    [{DetallesProducto}]{nota}";
        }
    }
}