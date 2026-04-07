# 🧰 NVM Manager

Gerenciador visual de ambientes **Node.js**, **Angular** e **NVM**, desenvolvido em **.NET 8** com **ASP.NET Core + WebView2**.  
Permite instalar, atualizar e sincronizar versões de Node e Angular de forma automática, segura e integrada.

---

## 📖 Sumário
- [Visão Geral](#visão-geral)
- [Arquitetura](#arquitetura)
- [Funcionalidades Principais](#funcionalidades-principais)
- [Fluxos Internos](#fluxos-internos)
- [Instalação e Execução](#instalação-e-execução)
- [Interface e Design](#interface-e-design)
- [Ciclos de Desenvolvimento](#ciclos-de-desenvolvimento)
- [Tecnologias Utilizadas](#tecnologias-utilizadas)
- [Contribuição](#contribuição)
- [Licença](#licença)

---

## 🧩 Visão Geral
O **NVM Manager** foi criado para simplificar o gerenciamento de ambientes Node.js e Angular em sistemas Windows.  
Ele detecta automaticamente versões instaladas, identifica incompatibilidades e realiza reinstalações seguras sem perda de contexto.

O sistema é dividido em duas camadas:
- **Desktop (WPF + WebView2)** — executável principal, com interface moderna e integração local.
- **Web (ASP.NET Core)** — camada de apresentação e controle, servindo páginas interativas.

---

## 🏗️ Arquitetura
NvmManager
├── NvmManager.Core        → Lógica de negócio e serviços
├── NvmManager.Web         → Interface web (ASP.NET Core)
└── NvmManager.Desktop     → Aplicação WPF com WebView2


### Principais componentes
- **AngularCommandExecutor** — executa comandos CLI e gerencia versões do Angular.
- **NvmApplicationService** — controla instalação e troca de versões do Node via NVM.
- **AngularApplicationService** — integra Node e Angular, garantindo compatibilidade.
- **Index.cshtml / Layout.cshtml** — interface principal do dashboard.

---

## ⚙️ Funcionalidades Principais
| Função | Descrição |
|--------|------------|
| **Detecção automática** | Identifica versões instaladas de Node, Angular e NVM. |
| **Instalação de Node LTS** | Baixa e ativa a versão mais recente e estável do Node.js. |
| **Instalação de Angular estável** | Obtém a última versão do Angular CLI diretamente do npm. |
| **Reinstalação inteligente** | Reinstala Angular após troca de Node sem perder contexto. |
| **Dashboard visual** | Exibe status do ambiente e permite ações rápidas. |
| **Gerenciamento de versões** | Permite instalar, remover e alternar versões do Node. |
| **Interface moderna** | Tema escuro, ícones minimalistas e animações suaves. |

---

## 🔄 Fluxos Internos

### Ciclo 7 — Reinstalação inteligente
1. Detecta incompatibilidade entre Node e Angular.  
2. Lê versão real do Angular via `package.json`.  
3. Remove Angular antigo.  
4. Instala novo Node.  
5. Reinstala Angular na mesma versão anterior.

### Ciclo 8 — Ambiente estável
1. Botões rápidos no dashboard para instalar Node e Angular LTS.  
2. Busca automática da última versão via NVM e npm.  
3. Instalação global e sincronizada.  
4. Feedback visual ao usuário.

---

## 🚀 Instalação e Execução

### Requisitos
- Windows 10 ou superior  
- .NET 8 SDK  
- NVM for Windows instalado  
- Permissões de administrador

### Execução
Execute o arquivo NvmManager.Desktop.exe

🎨 Interface e Design
Tema escuro com cores suaves (--bg, --panel, --card)

Ícones minimalistas para NVM, Node e Angular

Cards com hover e sombra para destaque

Sidebar com navegação clara e hierarquia visual

Feedback visual para sucesso e erro (.msg.ok / .msg.bad)

Ciclos de Desenvolvimento
Ciclo	Foco	Resultado
1–3	Estrutura base e serviços	Comunicação entre camadas
4–6	Detecção e compatibilidade	Diagnóstico de versões
7	Reinstalação inteligente	Correção automática de ambiente
8	Interface e UX	Dashboard moderno e intuitivo

🧠 Tecnologias Utilizadas
.NET 8 / C#

ASP.NET Core Razor Pages

WPF + WebView2

NVM for Windows

npm / Angular CLI

CSS moderno (grid, flex, variables)