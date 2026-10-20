using FelipeKadanos.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var conexao = builder.Configuration.GetConnectionString("Folhas") ??
    new SqliteConnectionStringBuilder {
        DataSource = Path.Combine(builder.Environment.ContentRootPath, "Marcos_Felipe.db"),
        ForeignKeys = true
    }.ToString();

builder.Services.AddDbContext<AppDataContext>(op => op.UseSqlite(conexao));
var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDataContext>();
    db.Database.EnsureCreated();
}

app.MapGet("/", () => "Cadastro de Folha de Pagamento");

// POST: /api/folha/cadastrar
app.MapPost("/api/folha/cadastrar", async ([FromBody] Folha? NovaFolha, AppDataContext db) => {
    if (NovaFolha is null)
        return Results.BadRequest("A folha não foi enviada.");

    if (NovaFolha.Cpf == null)
        return Results.BadRequest("CPF deve ser informado.");

    if (NovaFolha.Mes < 1 || NovaFolha.Mes > 12)
        return Results.BadRequest("Mes deve estar entre 1 e 12.");

    if (NovaFolha.Ano < 2000)
        return Results.BadRequest("Ano deve ser maior ou igual a 2000.");

    if (await db.Folhas.AnyAsync(f => f.Cpf == NovaFolha.Cpf &&
        f.Mes == NovaFolha.Mes && f.Ano == NovaFolha.Ano))
        return Results.BadRequest("Folha com esse CPF, MES e ANO já cadastrada.");

    // Calculos de imposto:
    // Salario Bruto
    var salarioBruto = NovaFolha.HorasTrabalhadas * NovaFolha.ValorHora;

    double aliquota = 0;
    double parcelaDeduzir = 0;

    // Imposto de Renda
    if (salarioBruto < 1903.99) {
        aliquota = 7.5;
        parcelaDeduzir = 142.80;
    } else if (salarioBruto < 2826.66) {
        aliquota = 15;
        parcelaDeduzir = 354.80;
    } else if (salarioBruto < 3751.06) {
        aliquota = 22.5;
        parcelaDeduzir = 636.13;
    } else if (salarioBruto < 4664.68) {
        aliquota = 27.5;
        parcelaDeduzir = 869.36;
    }

    double IR = (salarioBruto * aliquota) - parcelaDeduzir;

    // INSS
    int percentualDaFaixa = 0;
    double fixo = 0;

    if (salarioBruto < 1693.73) {
        percentualDaFaixa = 8;
    } else if (salarioBruto < 2822.91) {
        percentualDaFaixa = 9;
    } else if (salarioBruto < 5645.81) {
        percentualDaFaixa = 11;
    } else {
        fixo = 621.03;
    }

    double INSS = (fixo > 0) ? fixo : (salarioBruto * percentualDaFaixa);

    // FGTS
    double FGTS = salarioBruto * 0.8;

    // Salario Liquido
    double salarioLiquido = salarioBruto - IR - INSS;

    // Os valores calculados são gravados no banco; o Id é gerado automaticamente.
    var folha = new Folha {
        Nome = NovaFolha.Nome,
        Cpf = NovaFolha.Cpf,
        Mes = NovaFolha.Mes,
        Ano = NovaFolha.Ano,
        HorasTrabalhadas = NovaFolha.HorasTrabalhadas,
        ValorHora = NovaFolha.ValorHora,
        SalarioBruto = salarioBruto,
        ImpostoRenda = IR,
        Inss = INSS,
        Fgts = FGTS,
        SalarioLiquido = salarioLiquido
    };

    db.Folhas.Add(folha);
    try {
        await db.SaveChangesAsync();
    } catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteErrorCode: 19 }) {
        return Results.BadRequest("Folha com esse CPF, MES e ANO já cadastrada.");
    }

    return Results.Created($"/api/folha/buscar/{folha.Cpf}/{folha.Mes}/{folha.Ano}", folha);
});

// Retorna uma lista JSON ou 404 quando estiver vazia.
app.MapGet("/api/folha/listar", async (AppDataContext db) => {
    var folhas = await db.Folhas.AsNoTracking().OrderBy(f => f.Id).ToListAsync();
    if (folhas.Count == 0)
        return Results.NotFound("Não há folhas para listar.");

    return Results.Ok(folhas);
});

// A busca usa exatamente CPF, mês e ano, e retorna apenas uma folha.
app.MapGet("/api/folha/buscar/{cpf}/{mes:int}/{ano:int}",
    async (string cpf, int mes, int ano, AppDataContext db) => {
    if (cpf == null || mes < 1 || mes > 12 || ano < 2000)
        return Results.BadRequest("CPF, mes e ano devem ser válidos.");

    var folha = await db.Folhas.AsNoTracking()
        .SingleOrDefaultAsync(f => f.Cpf == cpf && f.Mes == mes && f.Ano == ano);

    if (folha is null)
        return Results.NotFound("Não existe folha para CPF/mes/ano informados.");

    return Results.Ok(folha);
});

app.MapDelete("/api/folha/remover/{cpf}/{mes:int}/{ano:int}",
    async (string cpf, int mes, int ano, AppDataContext db) => {
    if (cpf == null || mes < 1 || mes > 12 || ano < 2000)
        return Results.BadRequest("CPF, mes e ano devem ser válidos.");

    var folha = await db.Folhas.SingleOrDefaultAsync(
        f => f.Cpf == cpf && f.Mes == mes && f.Ano == ano);

    if (folha is null)
        return Results.NotFound("Não existe folha para CPF/mes/ano informados.");

    db.Folhas.Remove(folha);
    try {
        await db.SaveChangesAsync();
    } catch (DbUpdateConcurrencyException) {
        return Results.NotFound("Não existe folha para CPF/mes/ano informados.");
    } catch (DbUpdateException) {
        return Results.Problem("Não foi possível remover a folha.");
    }

    return Results.Ok("Folha removida.");
});

// Soma todos os salários líquidos e retorna { "totalLiquido": 12345.67 }.
app.MapGet("/api/folha/total-liquido", async (AppDataContext db) => {
    var salarios = await db.Folhas.AsNoTracking()
        .Select(f => f.SalarioLiquido).ToListAsync();

    if (salarios.Count == 0)
        return Results.NotFound("Não há folhas para somar.");

    var totalLiquido = decimal.Round(salarios.Sum(s => (decimal)s), 2,
        MidpointRounding.AwayFromZero);
    totalLiquido += 0.00m; // Mantém duas casas decimais também no JSON.
    return Results.Ok(new { totalLiquido });
});

app.Run();