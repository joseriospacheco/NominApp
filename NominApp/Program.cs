namespace NominApp;

using System;
using System.Globalization;
using System.IO;

class Program
{
    // ==============================
    // PARÁMETROS CONFIGURABLES
    // ==============================

    // Jornadas
    const int HORAS_MES_BASE = 240; // 8h * 30d (ajústalo a tu política)
    const decimal FACTOR_EXTRA_DIURNA = 1.25m;   // 25% recargo
    const decimal FACTOR_EXTRA_NOCTURNA = 1.75m; // 75% recargo

    // Deducciones (empleado)
    const decimal TASA_SALUD = 0.04m;   // 4%
    const decimal TASA_PENSION = 0.04m; // 4%

    // Prestaciones/otros (opcional, solo referencia; no se descuentan al neto en este ejemplo)
    // const decimal TASA_ARL = 0.00522m; // 0.522% (varía por riesgo)

    // Auxilio de transporte (ejemplo): aplica si salario básico <= UMBRAL_SALARIO_AUX_TRANS
    const decimal UMBRAL_SALARIO_AUX_TRANS = 2_000_000m; // ejemplo umbral (ajústalo)
    const decimal AUXILIO_TRANSPORTE_MENSUAL = 162_000m; // ejemplo valor (ajústalo)

    // ==============================
    // "Estructuras" con arreglos paralelos (sin clases de dominio)
    // ==============================
    const int MAX_EMPLEADOS = 200;

    static int[] ids = new int[MAX_EMPLEADOS];
    static string[] nombres = new string[MAX_EMPLEADOS];
    static decimal[] salarioBasicoMensual = new decimal[MAX_EMPLEADOS];

    static decimal[] horasTrabajadas = new decimal[MAX_EMPLEADOS];       // horas ordinarias del mes
    static decimal[] horasExtraDiurnas = new decimal[MAX_EMPLEADOS];
    static decimal[] horasExtraNocturnas = new decimal[MAX_EMPLEADOS];

    static int totalEmpleados = 0;
    static int nextId = 1;

    static void Main()
    {
        // Formato regional (por si estás en es-CO)
        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("es-CO");

        while (true)
        {
            Console.Clear();
            Console.WriteLine("=======================================");
            Console.WriteLine("        SISTEMA DE NÓMINA (Estruct.)   ");
            Console.WriteLine("=======================================");
            Console.WriteLine("1) Registrar empleado");
            Console.WriteLine("2) Listar empleados");
            Console.WriteLine("3) Registrar horas (ordinarias y extra)");
            Console.WriteLine("4) Calcular nómina de un empleado");
            Console.WriteLine("5) Calcular nómina general");
            Console.WriteLine("6) Exportar nómina general a CSV");
            Console.WriteLine("0) Salir");
            Console.Write("Seleccione opción: ");

            string op = Console.ReadLine()?.Trim() ?? "";
            switch (op)
            {
                case "1": RegistrarEmpleado(); break;
                case "2": ListarEmpleados(); Pausa(); break;
                case "3": RegistrarHoras(); break;
                case "4": CalcularNominaDeUno(); break;
                case "5": CalcularNominaGeneral(); break;
                case "6": ExportarNominaCSV(); break;
                case "0": return;
                default:
                    Console.WriteLine("Opción no válida.");
                    Pausa();
                    break;
            }
        }
    }

    // ==============================
    // Funciones de negocio (estructurado)
    // ==============================

    static void RegistrarEmpleado()
    {
        Console.Clear();
        Console.WriteLine("=== Registrar empleado ===");
        if (totalEmpleados >= MAX_EMPLEADOS)
        {
            Console.WriteLine("Capacidad máxima alcanzada.");
            Pausa();
            return;
        }

        Console.Write("Nombre completo: ");
        string nombre = Console.ReadLine()?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(nombre))
        {
            Console.WriteLine("Nombre inválido.");
            Pausa();
            return;
        }

        Console.Write("Salario básico mensual ($): ");
        if (!decimal.TryParse(Console.ReadLine(), out decimal salario) || salario <= 0)
        {
            Console.WriteLine("Salario inválido.");
            Pausa();
            return;
        }

        int pos = totalEmpleados;
        ids[pos] = nextId++;
        nombres[pos] = nombre;
        salarioBasicoMensual[pos] = salario;

        horasTrabajadas[pos] = 0;
        horasExtraDiurnas[pos] = 0;
        horasExtraNocturnas[pos] = 0;

        totalEmpleados++;

        Console.WriteLine($"Empleado registrado con ID: {ids[pos]}");
        Pausa();
    }

    static void ListarEmpleados()
    {
        Console.Clear();
        Console.WriteLine("=== Empleados ===");
        if (totalEmpleados == 0)
        {
            Console.WriteLine("No hay empleados.");
            return;
        }

        Console.WriteLine($"{"ID",4} | {"Nombre",-30} | {"Salario Básico",15}");
        Console.WriteLine(new string('-', 60));
        for (int i = 0; i < totalEmpleados; i++)
        {
            Console.WriteLine($"{ids[i],4} | {Trunc(nombres[i], 30),-30} | {salarioBasicoMensual[i],15:C0}");
        }
    }

    static void RegistrarHoras()
    {
        Console.Clear();
        Console.WriteLine("=== Registrar horas ===");
        if (totalEmpleados == 0)
        {
            Console.WriteLine("No hay empleados registrados.");
            Pausa();
            return;
        }

        int pos = BuscarEmpleadoPorIdPrompt();
        if (pos == -1) return;

        Console.Write("Horas ordinarias del mes: ");
        decimal horasOrd = LeerDecimalNoNegativo();

        Console.Write("Horas extra DIURNAS: ");
        decimal hxDiu = LeerDecimalNoNegativo();

        Console.Write("Horas extra NOCTURNAS: ");
        decimal hxNoc = LeerDecimalNoNegativo();

        horasTrabajadas[pos] = horasOrd;
        horasExtraDiurnas[pos] = hxDiu;
        horasExtraNocturnas[pos] = hxNoc;

        Console.WriteLine("Horas registradas correctamente.");
        Pausa();
    }

    static void CalcularNominaDeUno()
    {
        Console.Clear();
        Console.WriteLine("=== Cálculo de nómina (1 empleado) ===");
        if (totalEmpleados == 0)
        {
            Console.WriteLine("No hay empleados.");
            Pausa();
            return;
        }

        int pos = BuscarEmpleadoPorIdPrompt();
        if (pos == -1) return;

        MostrarLiquidacion(pos);
        Pausa();
    }

    static void CalcularNominaGeneral()
    {
        Console.Clear();
        Console.WriteLine("=== Nómina general ===");
        if (totalEmpleados == 0)
        {
            Console.WriteLine("No hay empleados.");
            Pausa();
            return;
        }

        decimal totalBruto = 0, totalDeds = 0, totalNeto = 0;

        Console.WriteLine($"{"ID",4} | {"Nombre",-25} | {"Bruto",12} | {"Deds",10} | {"Neto",12}");
        Console.WriteLine(new string('-', 75));

        for (int i = 0; i < totalEmpleados; i++)
        {
            CalcularValores(i,
                out decimal valorHora,
                out decimal pagoOrdinario,
                out decimal pagoExtras,
                out decimal auxTransporte,
                out decimal bruto,
                out decimal deducciones,
                out decimal neto);

            Console.WriteLine($"{ids[i],4} | {Trunc(nombres[i], 25),-25} | {bruto,12:C0} | {deducciones,10:C0} | {neto,12:C0}");

            totalBruto += bruto;
            totalDeds += deducciones;
            totalNeto += neto;
        }

        Console.WriteLine(new string('-', 75));
        Console.WriteLine($"{"",4}   {"Totales",-25}   {totalBruto,12:C0}   {totalDeds,10:C0}   {totalNeto,12:C0}");
        Pausa();
    }

    static void ExportarNominaCSV()
    {
        Console.Clear();
        Console.WriteLine("=== Exportar nómina a CSV ===");
        if (totalEmpleados == 0)
        {
            Console.WriteLine("No hay empleados.");
            Pausa();
            return;
        }

        Console.Write("Nombre de archivo (ej. nomina.csv): ");
        string nombre = Console.ReadLine()?.Trim() ?? "nomina.csv";
        if (!nombre.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            nombre += ".csv";

        using (var sw = new StreamWriter(nombre))
        {
            sw.WriteLine("ID,Nombre,SalarioBasico,ValorHora,HorasOrdinarias,HorasExtraDiurnas,HorasExtraNocturnas,AuxTransporte,Bruto,Deducciones,Neto");

            for (int i = 0; i < totalEmpleados; i++)
            {
                CalcularValores(i,
                    out decimal valorHora,
                    out decimal pagoOrdinario,
                    out decimal pagoExtras,
                    out decimal auxTransporte,
                    out decimal bruto,
                    out decimal deducciones,
                    out decimal neto);

                sw.WriteLine(string.Join(",",
                    ids[i],
                    EscaparCSV(nombres[i]),
                    salarioBasicoMensual[i].ToString("0.##"),
                    valorHora.ToString("0.##"),
                    horasTrabajadas[i].ToString("0.##"),
                    horasExtraDiurnas[i].ToString("0.##"),
                    horasExtraNocturnas[i].ToString("0.##"),
                    auxTransporte.ToString("0.##"),
                    bruto.ToString("0.##"),
                    deducciones.ToString("0.##"),
                    neto.ToString("0.##")
                ));
            }
        }

        Console.WriteLine($"Archivo generado: {nombre}");
        Pausa();
    }

    // ==============================
    // Cálculos
    // ==============================

    static void CalcularValores(
        int pos,
        out decimal valorHora,
        out decimal pagoOrdinario,
        out decimal pagoExtras,
        out decimal auxTransporte,
        out decimal bruto,
        out decimal deducciones,
        out decimal neto)
    {
        decimal salario = salarioBasicoMensual[pos];

        // Valor hora base a partir del salario mensual y horas base configuradas
        valorHora = salario / HORAS_MES_BASE;

        // Pago por horas ordinarias (proporcional si no trabajó todas)
        pagoOrdinario = valorHora * horasTrabajadas[pos];

        // Horas extra
        decimal pagoHxDiu = horasExtraDiurnas[pos] * valorHora * FACTOR_EXTRA_DIURNA;
        decimal pagoHxNoc = horasExtraNocturnas[pos] * valorHora * FACTOR_EXTRA_NOCTURNA;
        pagoExtras = RedondearMoneda(pagoHxDiu + pagoHxNoc);

        // Auxilio de transporte (si aplica por salario)
        auxTransporte = salario <= UMBRAL_SALARIO_AUX_TRANS ? AUXILIO_TRANSPORTE_MENSUAL : 0;

        // Bruto = ordinario + extras + aux.transporte
        bruto = RedondearMoneda(pagoOrdinario + pagoExtras + auxTransporte);

        // Deducciones (no incluyen aux. transporte)
        decimal baseDeducciones = pagoOrdinario + pagoExtras; // típico: aux. transporte no es base
        decimal salud = RedondearMoneda(baseDeducciones * TASA_SALUD);
        decimal pension = RedondearMoneda(baseDeducciones * TASA_PENSION);
        deducciones = salud + pension;

        // Neto
        neto = RedondearMoneda(bruto - deducciones);
    }

    static void MostrarLiquidacion(int pos)
    {
        CalcularValores(pos,
            out decimal valorHora,
            out decimal pagoOrdinario,
            out decimal pagoExtras,
            out decimal auxTransporte,
            out decimal bruto,
            out decimal deducciones,
            out decimal neto);

        Console.WriteLine($"Empleado: {nombres[pos]} (ID {ids[pos]})");
        Console.WriteLine($"Salario básico mensual: {salarioBasicoMensual[pos]:C0}");
        Console.WriteLine($"Valor hora: {valorHora:C0}");
        Console.WriteLine($"Horas ordinarias: {horasTrabajadas[pos]}  | Extra diurnas: {horasExtraDiurnas[pos]}  | Extra nocturnas: {horasExtraNocturnas[pos]}");
        Console.WriteLine(new string('-', 45));
        Console.WriteLine($"Pago ordinario: {pagoOrdinario:C0}");
        Console.WriteLine($"Pago horas extra: {pagoExtras:C0}");
        Console.WriteLine($"Auxilio transporte: {auxTransporte:C0}");
        Console.WriteLine($"BRUTO: {bruto:C0}");
        Console.WriteLine($"Deducciones (Salud {TASA_SALUD:P0} + Pensión {TASA_PENSION:P0}): {deducciones:C0}");
        Console.WriteLine($"NETO A PAGAR: {neto:C0}");
    }

    // ==============================
    // Utilidades
    // ==============================

    static int BuscarEmpleadoPorIdPrompt()
    {
        ListarEmpleados();
        Console.WriteLine();
        Console.Write("Ingrese ID del empleado: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("ID inválido.");
            Pausa();
            return -1;
        }

        int pos = -1;
        for (int i = 0; i < totalEmpleados; i++)
        {
            if (ids[i] == id) { pos = i; break; }
        }

        if (pos == -1)
        {
            Console.WriteLine("Empleado no encontrado.");
            Pausa();
        }

        return pos;
    }

    static decimal LeerDecimalNoNegativo()
    {
        string? s = Console.ReadLine();
        if (!decimal.TryParse(s, out decimal v) || v < 0)
        {
            Console.WriteLine("Valor inválido. Se toma 0.");
            return 0;
        }
        return v;
    }

    static string Trunc(string s, int len)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s.Length <= len ? s : s.Substring(0, len - 1) + "…";
    }

    static string EscaparCSV(string s)
    {
        if (s.Contains(",") || s.Contains("\""))
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }

    static decimal RedondearMoneda(decimal v) => Math.Round(v, 0, MidpointRounding.AwayFromZero);

    static void Pausa()
    {
        Console.WriteLine();
        Console.Write("Continuar... ");
        Console.ReadKey();
    }
}
