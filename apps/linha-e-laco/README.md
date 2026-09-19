# Linha & Laço

Aplicação de gestão para costureira.

- `web`: operação de clientes e pedidos.
- `backoffice`: métricas e administração.
- `api/LinhaELaco.Api`: API .NET 10 e recibos não fiscais.
- `supabase`: migrações da base isolada do produto.
- `docs/mvp-plan.md`: plano aprovado do MVP.

## Execução local

1. Execute `dotnet run` em `api/LinhaELaco.Api`.
2. Execute `npm install` e `npm run dev` em `web` e em `backoffice`.

Enquanto o Supabase não estiver conectado, a API usa dados de demonstração em memória para validar as telas e contratos.
