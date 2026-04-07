# 🧰 NVM Manager

Gerenciador visual de ambientes **Node.js**, **Angular CLI** e **NVM for Windows**, desenvolvido em **.NET 8** com **ASP.NET Core Razor Pages + WPF/WebView2**.  
Permite instalar, atualizar, alternar e remover versões de Node e Angular de forma automática, segura e integrada — tudo por meio de uma interface desktop moderna.

---

## 📖 Sumário

- [Visão Geral](#-visão-geral)
- [Arquitetura](#️-arquitetura)
- [Requisitos](#-requisitos)
- [Instalação e Execução](#-instalação-e-execução)
- [Navegação da Interface](#️-navegação-da-interface)
- [Fluxos do Usuário — NVM](#-fluxos-do-usuário--nvm)
- [Fluxos do Usuário — Node.js](#-fluxos-do-usuário--nodejs)
- [Fluxos do Usuário — Angular CLI](#️-fluxos-do-usuário--angular-cli)
- [Fluxo Especial — Ambiente Estável (LTS)](#-fluxo-especial--ambiente-estável-lts)
- [Fluxo Especial — Reinstalação Inteligente](#-fluxo-especial--reinstalação-inteligente)
- [Detecção Automática e Cache](#-detecção-automática-e-cache)
- [Tecnologias Utilizadas](#-tecnologias-utilizadas)
- [Licença](#-licença)

---

## 🧩 Visão Geral

O **NVM Manager** foi criado para simplificar o gerenciamento de ambientes Node.js e Angular em sistemas Windows.  
Ele detecta automaticamente versões instaladas, identifica incompatibilidades entre Node e Angular e realiza reinstalações seguras sem perda de contexto.

O sistema é composto por **três camadas**:

| Camada | Projeto | Responsabilidade |
|--------|---------|------------------|
| **Core** | `NvmManager.Core` | Lógica de negócio, services, entidades, interfaces e infraestrutura (execução de comandos NVM/npm/ng) |
| **Web** | `NvmManager.Web` | Interface de apresentação via ASP.NET Core Razor Pages, servindo o dashboard e páginas de gerenciamento |
| **Desktop** | `NvmManager.Desktop` | Aplicação WPF que hospeda o WebView2, inicia o servidor web embutido e exibe a UI para o usuário |

---

### Fluxo de inicialização da aplicação

```
Usuário abre NvmManager.Desktop.exe
    │
    ├─▶ WebHostProcessManager inicia NvmManager.Web.exe em background
    │       └─▶ Servidor ASP.NET Core sobe em http://127.0.0.1:5123
    │
    ├─▶ MainWindow aguarda o endpoint /health responder (polling de até 10s)
    │
    └─▶ WebView2 navega para http://127.0.0.1:5123 (Dashboard)
```

Ao fechar a janela, o processo do servidor web é encerrado automaticamente.

---

## 📋 Requisitos

- **Windows 10** ou superior
- **.NET 8 SDK** instalado
- **NVM for Windows** instalado (ou o sistema o instalará por você)
- **Permissões de administrador** (necessárias para o NVM manipular symlinks)

---

## 🚀 Instalação e Execução

```bash
# Compilar a solução
dotnet build NvmManager.sln

# Executar diretamente (modo desktop)
dotnet run --project NvmManager.Desktop
```

Ou execute o arquivo **`NvmManager.Desktop.exe`** publicado diretamente.

---

## 🗺️ Navegação da Interface

A interface possui uma **sidebar fixa** com três áreas de navegação:

| Ícone | Menu | Página | Descrição |
|:-----:|------|--------|-----------|
| 🧰 | **Dashboard** | `/` | Visão geral do ambiente — cards de status do NVM, Node e Angular + ações rápidas |
| 🟢 | **Node Hub** | `/Node/Versions` | Instalar, ativar e remover versões do Node.js |
| 🅰️ | **Angular Hub** | `/Angular/Index` | Instalar, remover e diagnosticar o Angular CLI |

Todas as páginas exibem **mensagens de feedback** (sucesso em verde, erro em vermelho) após cada ação do usuário.

---

## 🔧 Fluxos do Usuário — NVM

### 1. Verificar se o NVM está instalado

- **Onde:** Dashboard (card "NVM")
- **O que acontece:**
  1. O sistema verifica se o executável `nvm.exe` existe no caminho `%NVM_HOME%\nvm.exe`
  2. Executa `nvm version` para confirmar que está funcional
  3. Exibe um chip verde **"Instalado vX.Y.Z"** ou um chip vermelho **"Não encontrado"**

### 2. Instalar / Atualizar o NVM for Windows

- **Onde:** Dashboard → botão **"Gerenciar NVM"** → Página `/Nvm/Install`
- **Passos do usuário:**
  1. O usuário clica em **"Instalar / Atualizar NVM"**
  2. Mensagem de progresso é exibida
- **Fluxo interno:**
  1. Consulta a API do GitHub (`coreybutler/nvm-windows/releases/latest`) para obter a versão mais recente
  2. Localiza o asset `nvm-setup.exe` na release
  3. Faz download do instalador para o diretório temporário
  4. Executa `nvm-setup.exe /silent` (instalação silenciosa)
  5. Aguarda 2 segundos para propagação de variáveis de ambiente
  6. Verifica se `C:\nvm\nvm.exe` existe como confirmação
  7. Remove o instalador temporário
  8. Exibe mensagem de sucesso ou erro com detalhes

> ⚠️ **Nota:** Pode exigir permissões elevadas dependendo do ambiente.

---

## 🟢 Fluxos do Usuário — Node.js

### 3. Visualizar versões instaladas

- **Onde:** Node Hub (`/Node/Versions`) → seção **"Versões instaladas"**
- **O que acontece:**
  1. Executa `nvm list` para obter as versões instaladas
  2. Exibe uma tabela com: versão, status (badge **ATIVA** para a versão em uso), e botões de ação
  3. O usuário pode clicar em **"Atualizar"** para recarregar a lista

### 4. Instalar uma versão específica do Node.js

- **Onde:** Node Hub → seção **"Instalar nova versão"**
- **Passos do usuário:**
  1. Digita o número da versão (ex.: `18.17.0`) no campo de input
  2. Clica no botão **"Instalar"**
- **Fluxo interno:**
  1. Valida o formato da versão (deve ser `X.Y` ou `X.Y.Z`, somente números)
  2. Verifica se o NVM está instalado (caso contrário, retorna erro)
  3. Executa `nvm install <versão>`
  4. Verifica o output para confirmar instalação
  5. Recarrega a lista de versões
  6. Exibe mensagem de sucesso ou erro

### 5. Ativar (usar) uma versão do Node.js

- **Onde:** Node Hub → tabela de versões → botão **"Ativar"** (disponível para versões não ativas)
- **Passos do usuário:**
  1. Clica no botão **"Ativar"** da versão desejada
- **Fluxo interno:**
  1. Valida a string da versão
  2. Executa `nvm use <versão>`
  3. **Invalida o cache** de versão ativa (forçando reconsulta)
  4. Recarrega a lista de versões
  5. A versão ativada aparece com o badge **ATIVA**

### 6. Remover uma versão do Node.js

- **Onde:** Node Hub → tabela de versões → botão **"Remover"**
- **Passos do usuário:**
  1. Clica no botão **"Remover"** na versão desejada
  2. Um **modal de confirmação** aparece: _"Tem certeza que deseja remover a versão X.Y.Z?"_
  3. O usuário pode:
     - **"Cancelar"** → fecha o modal sem ação
     - **"Remover"** → confirma a remoção
- **Fluxo interno:**
  1. Executa `nvm uninstall <versão>`
  2. Invalida o cache de versão ativa
  3. Recarrega a lista de versões
  4. Exibe mensagem de sucesso ou erro

> ⚠️ **Essa ação não pode ser desfeita.** A versão é removida do disco permanentemente.

### 7. Instalar a versão estável (LTS) do Node.js

- **Onde:** Dashboard → card **"Ambiente Estável (LTS)"** → botão **"Instalar Node.js estável"**
- **Passos do usuário:**
  1. Clica em **"Instalar Node.js estável"**
- **Fluxo interno:**
  1. Executa `nvm list available` para listar versões remotas disponíveis
  2. Parseia e ordena todas as versões encontradas
  3. Seleciona a versão mais recente (maior número)
  4. Executa a instalação via `nvm install <versão>`
  5. Ativa automaticamente a versão instalada via `nvm use <versão>`
  6. Recarrega o dashboard com o novo estado

---

## 🅰️ Fluxos do Usuário — Angular CLI

### 8. Visualizar o Angular CLI instalado

- **Onde:** Angular Hub (`/Angular/Index`)
- **O que acontece:**
  1. Detecta a versão ativa do Node via `nvm current`
  2. Lê a versão do Angular diretamente do `package.json` em disco:
     ```
     %APPDATA%\nvm\v<nodeVersion>\node_modules\@angular\cli\package.json
     ```
  3. Exibe:
     - Chip **"Node em uso: vX.Y.Z"** no topo
     - Tabela com a versão do Angular instalada (se houver)
     - Mensagem _"Nenhuma versão do Angular CLI instalada"_ (se não houver)

### 9. Instalar uma versão específica do Angular CLI

- **Onde:** Angular Hub → seção **"Instalar Angular CLI"**
- **Passos do usuário:**
  1. Digita a versão desejada (ex.: `17.3.0`) no campo de input
  2. Clica no botão **"Instalar"**
- **Fluxo interno:**
  1. Executa `npm install -g @angular/cli@<versão>`
  2. Verifica o resultado da operação
  3. Recarrega a página com o novo estado
  4. Exibe mensagem de sucesso ou erro

### 10. Remover o Angular CLI

- **Onde:** Angular Hub → tabela de versões → botão **"Remover"**
- **Passos do usuário:**
  1. Clica no botão **"Remover"**
- **Fluxo interno:**
  1. Executa `npm uninstall -g @angular/cli`
  2. Recarrega a página
  3. Exibe mensagem de sucesso ou erro

### 11. Instalar a versão estável do Angular CLI

- **Onde:** Dashboard → card **"Ambiente Estável (LTS)"** → botão **"Instalar Angular CLI estável"**
- **Passos do usuário:**
  1. Clica em **"Instalar Angular CLI estável"**
- **Fluxo interno:**
  1. Verifica se há uma versão ativa do Node (obrigatório)
  2. Consulta a versão mais recente do Angular via `npm view @angular/cli version`
  3. Executa `npm install -g @angular/cli@<versão>`
  4. Recarrega o dashboard

### 12. Diagnosticar incompatibilidade Node × Angular

- **Onde:** Angular Hub (exibido automaticamente)
- **O que acontece:**
  1. Ao carregar a página, o sistema executa `ng version`
  2. Se o output contém _"requires a minimum Node.js version"_, ele detecta **incompatibilidade**
  3. Extrai a versão mínima do Node exigida pelo Angular
  4. Exibe um alerta vermelho com:
     - Mensagem: _"Angular CLI instalado, porém incompatível com o Node atual"_
     - Versão mínima requerida
     - Botão **"Atualizar Node"** para correção automática

---

## ⚡ Fluxo Especial — Ambiente Estável (LTS)

Este fluxo é o caminho mais rápido para configurar um ambiente funcional do zero.

```
Dashboard → Card "Ambiente Estável (LTS)"
    │
    ├─▶ [Botão] "Instalar Node.js estável"
    │       1. Consulta versões remotas (nvm list available)
    │       2. Identifica a mais recente
    │       3. Instala via nvm install
    │       4. Ativa via nvm use
    │       └── ✅ Node.js LTS pronto
    │
    └─▶ [Botão] "Instalar Angular CLI estável"
            1. Verifica se há Node ativo
            2. Consulta última versão via npm view @angular/cli version
            3. Instala via npm install -g @angular/cli@<versão>
            └── ✅ Angular CLI estável pronto
```

> 💡 **Ideal para:** estudos, testes e novos projetos. Para produção, utilize versões específicas conforme o projeto.

---

## 🔄 Fluxo Especial — Reinstalação Inteligente

Este é o fluxo mais complexo do sistema. Ele é acionado quando o Angular está **incompatível** com o Node atual e o usuário clica em **"Atualizar Node"**.

```
Angular Hub → Alerta de incompatibilidade → [Botão] "Atualizar Node"
    │
    ├─ 1. Detecta a versão sugerida do Node (baseada no erro do Angular)
    │
    ├─ 2. Lê a versão REAL do Angular instalada via package.json em disco
    │       └── Garante que a versão será preservada após a troca
    │
    ├─ 3. Remove o Angular CLI do Node atual
    │       └── npm uninstall -g @angular/cli
    │
    ├─ 4. Instala o novo Node compatível
    │       └── nvm install <versão sugerida>
    │
    ├─ 5. Ativa o novo Node
    │       └── nvm use <versão sugerida>
    │
    └─ 6. Reinstala o Angular na MESMA versão anterior
            └── npm install -g @angular/cli@<versão preservada>
            └── ✅ Ambiente atualizado e compatível
```

> 🛡️ Este fluxo garante que o usuário **não perde a versão do Angular** durante a troca de Node.

---

## 🧠 Detecção Automática e Cache

O sistema utiliza mecanismos inteligentes para otimizar a performance e evitar chamadas desnecessárias ao sistema operacional:

| Mecanismo | Descrição |
|-----------|-----------|
| **VersionCacheService** | Cache em memória da versão ativa do Node. Evita chamadas repetidas ao `nvm current`. Invalidado automaticamente após `use` ou `uninstall`. |
| **VersionStateCache** | Cache de estado completo: versão ativa + lista de versões instaladas. Invalidado após qualquer install/uninstall. |
| **Detecção via disco** | A versão do Angular é lida diretamente do `package.json` em `%APPDATA%\nvm\v<node>\node_modules\@angular\cli\`, sem executar processos. |
| **Fallback Node** | Se `nvm current` não retornar resultado, o sistema tenta `node --version` como fallback. |
| **Health check** | O Desktop aguarda o endpoint `/health` do servidor web antes de renderizar a interface. |

---

## 🛠️ Tecnologias Utilizadas

| Tecnologia | Uso |
|-----------|-----|
| **.NET 8 / C#** | Runtime e linguagem principal |
| **ASP.NET Core Razor Pages** | Camada web e renderização de UI |
| **WPF + WebView2** | Aplicação desktop com interface web embutida |
| **NVM for Windows** | Gerenciamento de versões do Node.js |
| **npm** | Instalação e remoção do Angular CLI |
| **Angular CLI (`ng`)** | Diagnóstico de compatibilidade |
| **GitHub Releases API** | Download automático do NVM for Windows |
| **CSS moderno** | Grid, Flexbox, variáveis CSS, tema escuro |

---

## 📄 Licença

Este projeto é de uso pessoal/educacional.