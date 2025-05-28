using LojaManoel.Models;
using Microsoft.EntityFrameworkCore;
using LojaManoel.Models;
using LojaManoel.Services;
using System.ComponentModel.DataAnnotations;

public class EmbalagemService : IEmbalagemService
{
    private readonly AppDbContext _context;
    private readonly ILogger<EmbalagemService> _logger;

    public EmbalagemService(AppDbContext context, ILogger<EmbalagemService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<EmbalagemOutput> ProcessarEmbalagem(EmbalagemInput input)
    {
        var embalagemOutput = new EmbalagemOutput();
        var caixasDisponiveisDb = await _context.Caixas.AsNoTracking().ToListAsync();

        if (!caixasDisponiveisDb.Any())
        {
            _logger.LogWarning("Nenhuma caixa disponível no banco de dados.");
            // Poderia lançar uma exceção ou retornar um erro específico
        }

        foreach (var pedidoInput in input.Pedidos)
        {
            if (pedidoInput.Produtos == null || !pedidoInput.Produtos.Any())
            {
                _logger.LogWarning("Pedido recebido sem produtos.");
                var pedidoOutputComErro = new PedidoOutput();
                embalagemOutput.Pedidos.Add(pedidoOutputComErro);
                continue;
            }

            var produtosDoPedido = pedidoInput.Produtos.Select(p => new Produto
            {
                Nome = p.Nome,
                Altura = p.Altura,
                Largura = p.Largura,
                Comprimento = p.Comprimento
            }).ToList();

            // --- Início da Lógica de Persistência ---
            var novoPedidoDb = new Pedido();
            // Os produtos são criados em memória primeiro, depois associados ao pedido e salvos.
            // Se os produtos já existissem no BD e fossem referenciados por ID, a lógica seria diferente.

            foreach (var produtoModel in produtosDoPedido)
            {
                novoPedidoDb.Produtos.Add(produtoModel);
            }
            _context.Pedidos.Add(novoPedidoDb);
            // --- Fim da Lógica de Persistência (parcial) ---


            var resultadoEmpacotamento = EmpacotarProdutosMelhorado(
                new List<Produto>(produtosDoPedido),
                caixasDisponiveisDb.Select(c => new Caixa
                {
                    Id = c.Id,
                    Nome = c.Nome,
                    Altura = c.Altura,
                    Largura = c.Largura,
                    Comprimento = c.Comprimento,
                    Produtos = new List<Produto>()
                }).ToList()
            );

            var pedidoOutput = new PedidoOutput();
            foreach (var caixaUsada in resultadoEmpacotamento.CaixasUtilizadas)
            {
                pedidoOutput.Caixas.Add(new CaixaOutput
                {
                    Nome = caixaUsada.Nome,
                    Produtos = caixaUsada.Produtos.Select(p => p.Nome).ToList()
                });

                // --- Lógica de Persistência para CaixaSelecionada ---
                var caixaSelecionadaDb = new CaixaSelecionada
                {
                    Nome = caixaUsada.Nome,
                    Pedido = novoPedidoDb,
                };
                foreach (var produtoNaCaixa in caixaUsada.Produtos)
                {
                    // Encontrar a referência da entidade Produto que já está sendo rastreada
                    var produtoEntidade = novoPedidoDb.Produtos
                        .FirstOrDefault(pdb => pdb.Nome == produtoNaCaixa.Nome &&
                                            pdb.Altura == produtoNaCaixa.Altura &&
                                            pdb.Largura == produtoNaCaixa.Largura &&
                                            pdb.Comprimento == produtoNaCaixa.Comprimento &&
                                            !_context.CaixasSelecionadas.Any(cs => cs.Produtos.Contains(pdb) && cs.PedidoId == novoPedidoDb.Id)); // Evitar adicionar o mesmo produto a múltiplas caixas se a lógica permitir (improvável com este algoritmo)

                    if (produtoEntidade != null)
                    {
                        caixaSelecionadaDb.Produtos.Add(produtoEntidade);
                    }
                    else
                    {
                        // Isso não deveria acontecer se todos os produtos em caixaUsada.Produtos vieram de produtosDoPedido
                        _logger.LogWarning($"Produto {produtoNaCaixa.Nome} da caixa usada não encontrado no pedido do banco de dados. Isso pode indicar um problema de rastreamento de entidade.");
                    }
                }
                _context.CaixasSelecionadas.Add(caixaSelecionadaDb);
                novoPedidoDb.Caixas.Add(caixaSelecionadaDb);
                // --- Fim da Lógica de Persistência para CaixaSelecionada ---
            }

            if (resultadoEmpacotamento.ProdutosNaoEmbalados.Any())
            {
                _logger.LogWarning($"Pedido {novoPedidoDb.Id} (a ser gerado) possui {resultadoEmpacotamento.ProdutosNaoEmbalados.Count} produtos não embalados: {string.Join(", ", resultadoEmpacotamento.ProdutosNaoEmbalados.Select(p => p.Nome))}");
                // Adicionar informação sobre produtos não embalados ao PedidoOutput
                // Ex: pedidoOutput.Erros.Add($"Produtos não embalados: {string.Join(", ", resultadoEmpacotamento.ProdutosNaoEmbalados.Select(p=>p.Nome))}");
            }

            embalagemOutput.Pedidos.Add(pedidoOutput);
        }
        await _context.SaveChangesAsync(); // Salva todas as alterações (Pedidos, Produtos associados, CaixasSelecionadas)
        return embalagemOutput;
    }

    private ResultadoEmpacotamento EmpacotarProdutosMelhorado(List<Produto> produtosParaEmpacotar, List<Caixa> caixasDisponiveisModelo)
    {
        var caixasUtilizadas = new List<Caixa>(); // Caixas que serão efetivamente usadas, com produtos dentro
        var produtosRestantes = new List<Produto>(produtosParaEmpacotar.OrderByDescending(p => p.Altura * p.Largura * p.Comprimento)); // Tenta empacotar os maiores primeiro (heurística)
        var produtosNaoEmbalados = new List<Produto>();

        // Ordena as caixas do menor para o maior volume para tentar usar as menores primeiro
        var caixasOrdenadasModelo = caixasDisponiveisModelo
            .OrderBy(c => c.Altura * c.Largura * c.Comprimento)
            .ToList();

        if (!caixasOrdenadasModelo.Any())
        {
            produtosNaoEmbalados.AddRange(produtosRestantes);
            produtosRestantes.Clear();
            return new ResultadoEmpacotamento { CaixasUtilizadas = caixasUtilizadas, ProdutosNaoEmbalados = produtosNaoEmbalados };
        }

        Caixa caixaAtual = null;

        while (produtosRestantes.Any())
        {
            var produtoAtual = produtosRestantes.First();
            bool produtoEmbaladoNestaIteracao = false;

            // Tentar colocar na caixa atual, se houver espaço
            if (caixaAtual != null && ProdutoCabeNaCaixaComProdutos(produtoAtual, caixaAtual))
            {
                caixaAtual.Produtos.Add(produtoAtual);
                produtosRestantes.Remove(produtoAtual);
                produtoEmbaladoNestaIteracao = true;
            }
            else // Tentar encontrar uma nova caixa (ou a primeira caixa)
            {
                caixaAtual = null; // Resetar caixa atual se o produto não coube ou se é o início
                foreach (var modeloCaixa in caixasOrdenadasModelo)
                {
                    // Tenta colocar na MENOR caixa disponível que comporte o produto
                    if (ProdutoCabeNaCaixa(produtoAtual, modeloCaixa)) // Checa apenas o produto, sem considerar outros já na caixa (pois é uma "nova" caixa)
                    {
                        // Cria uma NOVA instância de caixa baseada no modelo para ser usada
                        caixaAtual = new Caixa
                        {
                            Id = modeloCaixa.Id,
                            Nome = modeloCaixa.Nome,
                            Altura = modeloCaixa.Altura,
                            Largura = modeloCaixa.Largura,
                            Comprimento = modeloCaixa.Comprimento,
                            Produtos = new List<Produto>() // Importante: nova lista de produtos
                        };
                        caixaAtual.Produtos.Add(produtoAtual);
                        caixasUtilizadas.Add(caixaAtual); // Adiciona à lista de caixas utilizadas
                        produtosRestantes.Remove(produtoAtual);
                        produtoEmbaladoNestaIteracao = true;
                        break; // Achou uma caixa para o produto atual, sai do loop de modelos de caixa
                    }
                }
            }

            if (!produtoEmbaladoNestaIteracao)
            {
                produtosNaoEmbalados.Add(produtoAtual);
                produtosRestantes.Remove(produtoAtual);
                caixaAtual = null; // Força a busca por uma nova caixa para o próximo produto (se houver)
            }
        }

        return new ResultadoEmpacotamento { CaixasUtilizadas = caixasUtilizadas, ProdutosNaoEmbalados = produtosNaoEmbalados };
    }

    // Método auxiliar para verificar se um produto cabe numa caixa que JÁ CONTÉM outros produtos.
    private bool ProdutoCabeNaCaixaComProdutos(Produto produto, Caixa caixa)
    {
        if (caixa.Produtos.Count >= 5) return false;
        return ProdutoCabeNaCaixa(produto, caixa);
    }

    private bool ProdutoCabeNaCaixa(Produto produto, Caixa caixa)
    {
        decimal pAltura = produto.Altura;
        decimal pLargura = produto.Largura;
        decimal pComprimento = produto.Comprimento;

        decimal cAltura = caixa.Altura;
        decimal cLargura = caixa.Largura;
        decimal cComprimento = caixa.Comprimento;

        // Checa todas as 6 orientações do produto
        if ((pAltura <= cAltura && pLargura <= cLargura && pComprimento <= cComprimento) ||
            (pAltura <= cAltura && pComprimento <= cLargura && pLargura <= cComprimento) ||
            (pLargura <= cAltura && pAltura <= cLargura && pComprimento <= cComprimento) ||
            (pLargura <= cAltura && pComprimento <= cLargura && pAltura <= cComprimento) ||
            (pComprimento <= cAltura && pAltura <= cLargura && pLargura <= cComprimento) ||
            (pComprimento <= cAltura && pLargura <= cLargura && pAltura <= cComprimento))
        {
            return true;
        }
        return false;
    }
}

public class ResultadoEmpacotamento
{
    public List<Caixa> CaixasUtilizadas { get; set; } = new List<Caixa>();
    public List<Produto> ProdutosNaoEmbalados { get; set; } = new List<Produto>();
}