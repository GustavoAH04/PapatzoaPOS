using System;

namespace PapatzoaPOS.Modelos
{
    // PILAR 3: HERENCIA (BebidaArtesanal "es un" Producto)
    public class BebidaArtesanal : Producto
    {
        private string metodoPreparacion = string.Empty;
        private string tamano = string.Empty;
        private string endulzante = string.Empty;

        public BebidaArtesanal(string codigo, string nombre, double precioBase, int stock,
                               string metodoPreparacion, string tamano = "Mediano",
                               string endulzante = "Piloncillo", int stockMinimo = 8)
            : base(codigo, nombre, precioBase, stock, stockMinimo)
        {
            MetodoPreparacion = metodoPreparacion;
            Tamano = tamano;
            Endulzante = endulzante;
        }

        public string MetodoPreparacion
        {
            get { return metodoPreparacion; }
            set { metodoPreparacion = string.IsNullOrWhiteSpace(value) ? "Tradicional" : value.Trim(); }
        }

        public string Tamano
        {
            get { return tamano; }
            set
            {
                string valor = string.IsNullOrWhiteSpace(value) ? "Mediano" : value.Trim();
                tamano = valor;
            }
        }

        public string Endulzante
        {
            get { return endulzante; }
            set { endulzante = string.IsNullOrWhiteSpace(value) ? "Natural" : value.Trim(); }
        }

        public override string Categoria => "Bebida";

        // Comportamiento polimórfico: ajusta el precio según el tamaño de la taza o jarrito
        public override double CalcularPrecioFinal()
        {
            double extraTamano = 0;

            if (Tamano.Equals("Mediano", StringComparison.OrdinalIgnoreCase))
                extraTamano = 8.0;
            else if (Tamano.Equals("Grande", StringComparison.OrdinalIgnoreCase))
                extraTamano = 15.0;

            return Math.Round(PrecioBase + extraTamano, 2);
        }

        public override string ObtenerDetallesEspecificos()
        {
            return $"{MetodoPreparacion} | {Tamano} | Endulzante: {Endulzante}";
        }
    }
}