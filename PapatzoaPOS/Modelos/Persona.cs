using System;

namespace PapatzoaPOS.Modelos
{
    // PILAR 1: ABSTRACCIÓN
    // 'abstract' impide crear una Persona genérica; obliga a usar Cliente o Proveedor
    public abstract class Persona
    {
        // PILAR 2: ENCAPSULACIÓN (atributos privados protegidos)
        private string id = string.Empty;
        private string nombre = string.Empty;
        private string telefono = string.Empty;
        private string correo = string.Empty;

        protected Persona(string id, string nombre, string telefono, string correo)
        {
            Id = id;
            Nombre = nombre;
            Telefono = telefono;
            Correo = correo;
        }

        // Propiedades (equivalente moderno a get/set de C++)
        public string Id
        {
            get { return id; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("El ID no puede estar vacío.");
                id = value.Trim();
            }
        }

        public string Nombre
        {
            get { return nombre; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("El nombre es obligatorio.");
                nombre = value.Trim();
            }
        }

        public string Telefono
        {
            get { return telefono; }
            set { telefono = string.IsNullOrWhiteSpace(value) ? "Sin teléfono" : value.Trim(); }
        }

        public string Correo
        {
            get { return correo; }
            set { correo = string.IsNullOrWhiteSpace(value) ? "Sin correo" : value.Trim(); }
        }

        // PILAR 4: POLIMORFISMO (método abstracto = función virtual pura en C++)
        public abstract string ObtenerResumen();
    }
}