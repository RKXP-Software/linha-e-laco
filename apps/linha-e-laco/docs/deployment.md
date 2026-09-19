# Publicação do Linha & Laço

## Antes de publicar

A API atual usa dados de demonstração em memória. Ela pode validar o pipeline, mas não é produção até a integração Supabase e autenticação estarem concluídas.

## Git

1. Crie um repositório privado no GitHub ou GitLab, sem README ou `.gitignore` inicial.
2. No diretório raiz deste projeto, associe o remoto e envie a branch principal:

```powershell
git remote add origin <URL_DO_REPOSITORIO>
git push -u origin main
```

Arquivos `.env` são ignorados; versione apenas `.env.example`.

## Vercel

Importe o mesmo repositório duas vezes, criando projetos independentes:

| Projeto | Root Directory | Variável de produção e preview |
| --- | --- | --- |
| `linha-e-laco-web` | `apps/linha-e-laco/web` | `VITE_API_URL=https://<dominio-da-api>/api` |
| `linha-e-laco-backoffice` | `apps/linha-e-laco/backoffice` | `VITE_API_URL=https://<dominio-da-api>/api` |

O Vercel detecta Vite e usa os arquivos `vercel.json`. Depois do primeiro deploy da API, redeploy os dois projetos para incorporar a URL pública.

## Railway

1. Crie um serviço a partir do mesmo repositório.
2. Defina **Root Directory** como `apps/linha-e-laco/api/LinhaELaco.Api`.
3. O Dockerfile e `railway.json` serão encontrados nesse diretório; o healthcheck será `/health`.
4. Gere um domínio público da API e use-o como `VITE_API_URL` nos dois projetos Vercel.
5. Cadastre futuros segredos do Supabase somente em Railway Variables, a partir de `.env.example`.

## Supabase

1. Crie um projeto Supabase exclusivo para Linha & Laço.
2. A partir de `apps/linha-e-laco`, inicialize e vincule o Supabase CLI antes do primeiro push de banco.
3. Mantenha mudanças de schema em `supabase/migrations/`; a migração inicial já está versionada.
4. Antes de aplicar em produção, use `supabase db push --dry-run`; depois aplique com `supabase db push`.

Não envie seed data, senhas, connection strings ou chaves privadas para produção. No Railway, use `SUPABASE_URL`, `SUPABASE_PUBLISHABLE_KEY`, `SUPABASE_SECRET_KEY` (quando a API precisar dela) e `ConnectionStrings__Supabase` exclusivamente em **Variables**. As chaves `anon` e `service_role` são legadas.