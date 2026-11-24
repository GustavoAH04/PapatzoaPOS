using System;

namespace PapatzoaPOS.Modelos
{
    public class Cliente : Persona
    {
        private int sellosAcumulados;
        public DateTime FechaRegistro { get; private set; }

        public Cliente(string id, string nombre, string telefono, string correo, int sellosIniciales = 0)
            : base(id, nombre, telefono, correo)
        {
            SellosAcumulados = sellosIniciales;
            FechaRegistro = DateTime.Now;
        }

        public int SellosAcumulados
        {
            get { return sellosAcumulados; }
            set
            {
                if (value < 0)
                    throw new ArgumentException("Los sellos no pueden ser negativos.");
                sellosAcumulados = value;
            }
        }

        public void AgregarSello()
        {
            sellosAcumulados++;
        }

        public bool TieneCortesiaDisponible()
        {
            return sellosAcumulados >= 5;
        }

        public bool CanjearCortesia()
        {
            if (TieneCortesiaDisponible())
            {
                sellosAcumulados -= 5;
                return true;
            }
            return false;
        }

        public override string ObtenerResumen()
        {
            string estadoCortesia = TieneCortesiaDisponible() ? " [¡CORTESÍA DISPONIBLE!]" : "";
            return $"Cliente: {Nombre} (ID: {Id}) | Sellos Apapacho: {SellosAcumulados}/5{estadoCortesia}";
        }
    }
}