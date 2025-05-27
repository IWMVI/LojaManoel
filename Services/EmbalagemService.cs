using LojaManoel.Models;
using LojaManoel.Services;
using Microsoft.EntityFrameworkCore;
public class EmbalagemService : IEmbalagemService
{
    private readonly AppDbContext _context;

    public EmbalagemService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<EmbalagemOutput> ProcessarEmbalagem(EmbalagemInput input)
    {
        var output = new EmbalagemOutput();
        var caixasDisponiveis = await _context.Caixas.ToListAsync();

        foreach (var pedidoInput in input.Pedidos)
        {
            var pedidoOutput = new PedidoOutput();
            var produtos = pedidoInput.Produtos.Select(p => new Produto
            {
                Nome = p.Nome,
                Altura = p.Altura,
                Largura = p.Largura,
                Comprimento = p.Comprimento
            }).ToList();

            var caixasUsadas = EmpacotarProdutos(produtos, caixasDisponiveis);

            foreach (var caixa in caixasUsadas)
            {
                pedidoOutput.Caixas.Add(new CaixaOutput
                {
                    Nome = caixa.Nome,
                    Produtos = caixa.Produtos.Select(p => p.Nome).ToList()
                });
            }

            output.Pedidos.Add(pedidoOutput);
        }

        return output;
    }

    private List<Caixa> EmpacotarProdutos(List<Produto> produtos, List<Caixa> caixasDisponiveis)
    {
        var caixasUsadas = new List<Caixa>();
        var produtosRestantes = new List<Produto>(produtos);

        // Ordena as caixas do menor para o maior volume para tentar usar as menores primeiro
        var caixasOrdenadas = caixasDisponiveis.OrderBy(c => c.Altura * c.Largura * c.Comprimento).ToList();

        while (produtosRestantes.Any())
        {
            var produtoAtual = produtosRestantes.First();
            var caixaEncontrada = false;

            foreach (var caixa in caixasOrdenadas)
            {
                // Verifica se o produto cabe na caixa em qualquer orientação
                if (ProdutoCabeNaCaixa(produtoAtual, caixa))
                {
                    // Cria uma cópia da caixa para adicionar à lista de caixas usadas
                    var caixaUsada = new Caixa
                    {
                        Nome = caixa.Nome,
                        Altura = caixa.Altura,
                        Largura = caixa.Largura,
                        Comprimento = caixa.Comprimento
                    };

                    caixaUsada.Produtos.Add(produtoAtual);
                    caixasUsadas.Add(caixaUsada);
                    produtosRestantes.Remove(produtoAtual);
                    caixaEncontrada = true;
                    break;
                }
            }

            if (!caixaEncontrada)
            {
                // Se não encontrou caixa para o produto, usa a maior caixa disponível
                var maiorCaixa = caixasOrdenadas.Last();
                var caixaUsada = new Caixa
                {
                    Nome = maiorCaixa.Nome,
                    Altura = maiorCaixa.Altura,
                    Largura = maiorCaixa.Largura,
                    Comprimento = maiorCaixa.Comprimento
                };

                caixaUsada.Produtos.Add(produtoAtual);
                caixasUsadas.Add(caixaUsada);
                produtosRestantes.Remove(produtoAtual);
            }
        }

        return caixasUsadas;
    }

    private bool ProdutoCabeNaCaixa(Produto produto, Caixa caixa)
    {
        // Obtém todas as permutações possíveis das dimensões do produto
        var dimensoesProduto = new List<(decimal, decimal, decimal)>
        {
            (produto.Altura, produto.Largura, produto.Comprimento),
            (produto.Altura, produto.Comprimento, produto.Largura),
            (produto.Largura, produto.Altura, produto.Comprimento),
            (produto.Largura, produto.Comprimento, produto.Altura),
            (produto.Comprimento, produto.Altura, produto.Largura),
            (produto.Comprimento, produto.Largura, produto.Altura)
        };

        // Verifica se alguma permutação cabe na caixa
        return dimensoesProduto.Any(dimensao =>
            dimensao.Item1 <= caixa.Altura &&
            dimensao.Item2 <= caixa.Largura &&
            dimensao.Item3 <= caixa.Comprimento);
    }
}