# Steam Farm — Windows 11

Interface modernizada do BDK Steam Hour Farm, com janela compacta de tamanho fixo, sem maximização, e temas claro e escuro.

## Download — v1.2.0

**[Baixar o aplicativo para Windows x64](https://github.com/walmirramos/Steam-Farm/raw/refs/heads/main/Release/BDK-Steam-Hour-Farm-Windows11-v1.2.0.zip)**

Extraia a pasta inteira e execute `BDKSteamHourFarm.exe`. Mantenha o auxiliar, suas bibliotecas e os arquivos de configuração na mesma pasta.

Requisitos: Windows x64, .NET Framework 4.8 e Steam aberta com uma conta conectada.

## Novidades

- Janela fixa de 660 × 574 unidades lógicas, com configuração de escala por monitor.
- Tema claro ou escuro, salvo entre sessões.
- Lista de jogos com AppID, status e tempo local.
- Controles para parar um jogo selecionado ou todos os jogos desta sessão.
- Inicialização assíncrona, mantendo a janela responsiva.
- Cancelamento dos jogos ainda na fila pelo botão **Parar todos**.
- Validação dos AppIDs e remoção de duplicatas antes de iniciar.
- Jogos já ativos são mantidos ao iniciar novos IDs.
- Contador que continua após 24 horas.
- Encerramento restrito aos processos iniciados pela própria janela.
- Confirmação ao fechar uma sessão ativa.
- Registros e preferências em `%LOCALAPPDATA%\BDKSteamHourFarm-Modern`.

O tempo exibido mede a sessão local. O aplicativo não consulta as horas creditadas pela Steam. **Em execução** indica que o processo auxiliar continua ativo.

## Como usar

1. Abra a Steam e entre na sua conta.
2. Abra `BDKSteamHourFarm.exe`.
3. Informe os AppIDs, separados por vírgula, espaço, ponto e vírgula ou quebra de linha.
4. Clique em **Iniciar jogos**.
5. Selecione uma linha para usar **Parar selecionado**, ou clique em **Parar todos**.

As preferências da interface antiga não são importadas automaticamente. Informe seus AppIDs uma vez na nova versão.

## Compilar a interface

Use um SDK .NET com suporte a projetos SDK-style e os assemblies de referência do .NET Framework 4.8. A entrega foi compilada com o SDK .NET 10.0.403.

Na raiz do repositório, execute:

```powershell
Expand-Archive -LiteralPath '.\Release\BDK-Steam-Hour-Farm-Windows11-v1.2.0.zip' -DestinationPath '.\build\runtime' -Force
.\build-modern.ps1 -RuntimeDirectory '.\build\runtime\BDK-Steam-Hour-Farm-Windows11'
```

O resultado fica em `Source/SteamIdleManager/bin/Release/net48/BDKSteamHourFarm.exe`.

A interface continua usando .NET Framework 4.8. O parâmetro `RuntimeDirectory` copia o auxiliar e suas dependências para a saída. O pacote conserva os componentes auxiliares da versão original.

## Testes

```powershell
.\test-modern.ps1
```

São 21 verificações, usando um processo auxiliar simulado, sem iniciar jogos reais: validação, deduplicação, tempo acima de 24 horas, dependências ausentes, início, reinício, parada individual/coletiva, códigos de saída, cancelamento e preservação de outras instâncias.

Validação desta entrega: compilação Release sem erros ou avisos, 21 verificações aprovadas e inspeção visual dos temas claro/escuro e da entrada vazia. O crédito de horas na Steam e a escala em múltiplos monitores não foram testados.

## Estrutura e origem

- `Source/SteamIdleManager`: interface modernizada.
- `Tests`: testes e auxiliar simulado, que não deve substituir o auxiliar real.
- `Release`: pacotes prontos para uso, incluindo as versões anteriores.
- `steam-idle Source` e `Dependencies`: componentes originais preservados.

Baseado no [BDK-Steam-Hour-Farm de arthurqui](https://github.com/arthurqui/BDK-Steam-Hour-Farm), commit `b3b5a7e81fec3167a0291027700dc3f4be50c073`. Esta versão é uma adaptação mantida neste repositório, não uma publicação oficial do autor original.

O [README original](README-original.md) foi preservado para referência e créditos. Para a versão modernizada, use `build-modern.ps1` e `test-modern.ps1`; os scripts antigos permanecem como parte do histórico do projeto.
