---
paths:
  - "src/Equinox.Domain/**"
---
# Regras do domínio (src/Equinox.Domain)
- O domínio não referencia Application, Infra nem UI, e não ganha pacote NuGet novo.
- Regra de negócio é validada na entidade e devolvida como notificação, nunca como exceção.
- Termine toda resposta que envolver arquivos desta pasta com a linha: [regra do domínio carregada]