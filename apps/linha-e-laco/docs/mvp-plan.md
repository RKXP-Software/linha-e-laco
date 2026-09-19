# Linha & Laço — plano do MVP

## Objetivo

Sistema para uma costureira brasileira controlar clientes, serviços, confecções, pedidos, parcelas e métricas. A aplicação de operação e o back-office são frontends React independentes; a API é ASP.NET Core .NET 10 no Railway; cada produto usa um projeto Supabase próprio.

## Decisões fechadas

- Público inicial: somente a costureira, com uma conta administradora.
- Fluxo: orçamento, aprovado, em produção, pronto, entregue ou cancelado.
- Serviço: trabalho sobre peça existente, com unidade, instruções, preço e prazo.
- Confecção: peça sob medida, com medidas, materiais, aviamentos, ficha técnica e etapas.
- Pedido: guarda medidas, anexos, ficha técnica, itens, parcelas e histórico.
- Página inicial: bloco de anotações para futuros clientes ou pedidos, com criação, conclusão e exclusão.
- Documento: recibo não fiscal em PDF para download. Nota fiscal, e-mail e WhatsApp ficam para depois.
- Métricas: recebimentos, previsão, atrasos, ticket médio, clientes ativos, status, itens mais vendidos e conversão.

## Critérios de aceite

A costureira deve conseguir criar cliente, cadastrar os dois tipos de item, montar pedido, alterar status, registrar parcelas e baixar recibo. As duas interfaces devem funcionar em celular, tablet e desktop.
