using System;

namespace PapatzoaPOS.Modelos
{
    // PILAR 1: ABSTRACCIÓN
    // Define la plantilla obligatoria de cualquier artículo que venda Papatzoa
    public abstract class Producto
    {
        // PILAR 2: ENCAPSULACIÓN
        private string codigo = string.Empty;
        private string nombre = string.Empty;
        private double precioBase;
        private int stock;
        private int stockMinimo;

        protected Producto(string codigo, string nombre, double precioBase, int stock, int stockMinimo = 5)
        {
            Codigo = codigo;
            Nombre = nombre;
            PrecioBase = precioBase;
            Stock = stock;
            StockMinimo = stockMinimo;
        }

        public string Codigo
        {
            get { return codigo; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("El código del producto es obligatorio.");
                codigo = value.Trim().ToUpper();
            }
        }

        public string Nombre
        {
            get { return nombre; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("El nombre del producto es obligatorio.");
                nombre = value.Trim();
            }
        }

        public double PrecioBase
        {
            get { return precioBase; }
            set
            {
                if (value <= 0)
                    throw new ArgumentException("El precio base debe ser mayor a cero.");
                precioBase = Math.Round(value, 2);
            }
        }

        public int Stock
        {
            get { return stock; }
            set
            {
                if (value < 0)
                    throw new ArgumentException("El stock no puede ser negativo.");
                stock = value;
            }
        }

        public int StockMinimo
        {
            get { return stockMinimo; }
            set
            {
                if (value < 0)
                    throw new ArgumentException("El stock mínimo no puede ser negativo.");
                stockMinimo = value;
            }
        }

        // Métodos de negocio compartidos por todos los productos
        public bool TieneStockBajo()
        {
            return stock <= stockMinimo;
        }

        public bool DescontarStock(int cantidad)
        {
            if (cantidad <= 0 || cantidad > stock)
                return false;

            stock -= cantidad;
            return true;
        }

        public void AgregarStock(int cantidad)
        {
            if (cantidad <= 0)
                throw new ArgumentException("La cantidad a reabastecer debe ser mayor a cero.");

            stock += cantidad;
        }

        // PILAR 4: POLIMORFISMO (cada tipo de producto define su propia categoría, precio y detalle)
        public abstract string Categoria { get; }
        public abstract double CalcularPrecioFinal();
        public abstract string ObtenerDetallesEspecificos();

        public virtual string ObtenerDescripcionTicket()
        {
            return $"{Nombre} ({ObtenerDetallesEspecificos()})";
        }
    }
}