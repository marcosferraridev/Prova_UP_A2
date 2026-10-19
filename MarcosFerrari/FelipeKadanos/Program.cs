
using FelipeKadanos.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Cadastro de Folha de Pagamento");

List<Folha> cadFolhas = new() {};

// POST: http://localhost/api/folha/cadastrar
app.MapPost("/api/folha/cadastrar", ([FromBody] Folha? NovaFolha) => { 
    if (NovaFolha is null)
        return Results.BadRequest("A folha não foi enviada.");
    
    foreach (var cadFolha in cadFolhas) {
        if (cadFolha.Cpf == NovaFolha.Cpf && cadFolha.Mes == NovaFolha.Mes && cadFolha.Ano == NovaFolha.Ano)
            return Results.BadRequest("Folha com esse CPF, MES e ANO já cadastrada.");
    }

    if (NovaFolha.Mes > 0 && NovaFolha.Mes < 13) {
        return Results.BadRequest("Mes deve estar entre 1 e 12.");
    }
    
    if (NovaFolha.Ano > 1999) {
        return Results.BadRequest("Ano deve ser maior que 2000.");
    }

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
    
    cadFolhas.Add(NovaFolha);
    return Results.Created("", NovaFolha);
    // return Results.Created($"/api/folha/{folha.Id}", folha);
});

app.Run();

/* 

app.MapPost("/projetos", async (NomeRequest body, AppDataContext db) => {
    if (string.IsNullOrWhiteSpace(body.Nome) || body.Nome.Trim().Length > 100)
        return Results.BadRequest("Nome é obrigatório e deve ter até 100 caracteres.");
        
    var item = new Projeto { Nome = body.Nome.Trim() };
    db.Projetos.Add(item);

    await db.SaveChangesAsync();
    return Results.Created($"/projetos/{item.Id}", item);
});

app.MapPut("/projetos/{id:int}", async (int id, NomeRequest body, AppDataContext db) => {
    var item = await db.Projetos.FindAsync(id);
    
    if (item is null) return Results.NotFound();

    if (string.IsNullOrWhiteSpace(body.Nome) || body.Nome.Trim().Length > 100)
        return Results.BadRequest("Nome é obrigatório e deve ter até 100 caracteres.");

    item.Nome = body.Nome.Trim();
    await db.SaveChangesAsync();

    return Results.Ok(item);
});

app.MapDelete("/projetos/{id:int}", async (int id, AppDataContext db) => {
    var item = await db.Projetos.FindAsync(id);
    
    if (item is null) return Results.NotFound();
    
    if (await db.Tarefas.AnyAsync(t => t.ProjetoId == id))
        return Results.Conflict("Projeto possui tarefas vinculadas.");
        
    db.Projetos.Remove(item);
    
    try { 
        await db.SaveChangesAsync();
    } catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteExtendedErrorCode: 787 or 1811 }) { 
        return Results.Conflict("Projeto possui tarefas vinculadas."); 
    }

    return Results.NoContent();
}); */