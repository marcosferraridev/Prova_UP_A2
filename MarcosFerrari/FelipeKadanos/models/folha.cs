namespace FelipeKadanos.models;
public class Folha {
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Cpf { get; set; }
    public int Mes { get; set; }
    public int Ano { get; set; }
    public int HorasTrabalhadas { get; set; }
    public double ValorHora { get; set; }
    public double SalarioBruto { get; set; }
    public double ImpostoRenda { get; set; }
    public double Inss { get; set; }
    public double Fgts { get; set; }
    public double SalarioLiquido { get; set; }
}
