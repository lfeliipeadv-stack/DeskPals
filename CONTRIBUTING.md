# Como contribuir

Obrigado por querer ajudar o Meu Pet! Há três jeitos principais.

## Um pet novo para a galeria

1. Abra o **Editor de Pets** (Configurações › Programa › Editor de Pets, ou o `PetEditor.exe`).
2. Crie o pet com o **Assistente de pet novo** ou mude um que já existe (use **Salvar como** para não mexer no original).
3. Teste com **Testar no desktop** (F5): ele abre um segundo DesktopPet só com o seu pet, sem mexer no seu.
4. Confira o **Verificador**: a galeria só recebe pets sem erros, com nome e autor no Cabeçalho.
5. Clique em **Publicar na galeria**: o editor cria o .zip (em `Documentos\Meu Pet\galeria`) e abre o
   [formulário de pet](https://github.com/lfeliipeadv-stack/meu-pet/issues/new?template=new_pet.md) já preenchido.
   Arraste o .zip para a página e envie.

Use desenhos seus ou com licença que permita a distribuição, e diga de onde vieram a arte e os sons.
O passo a passo completo está em [Crie o seu pet](https://lfeliipeadv-stack.github.io/meu-pet/wiki/Crie-o-seu-pet.html).

## Uma tradução

Os idiomas do programa ficam em `src/lang` (os que vêm com ele) e em `Languages` (os que se baixam nas
configurações). Cada arquivo é um JSON de chave → texto. Para traduzir um idioma novo ou corrigir um que já existe,
siga [Languages/README.md](Languages/README.md) e abra um pull request.

## Um problema ou uma ideia

Abra uma [issue](https://github.com/lfeliipeadv-stack/meu-pet/issues/new/choose) com o formulário de problema ou de
ideia. Para um problema, diga a versão (Configurações › Sobre), o Windows e como repetir.

## Código

O programa é C# (Windows Forms) sobre .NET 10. Antes de mandar um pull request:

```bash
dotnet build src/DesktopPet.Net.csproj -c Release
src\bin\Release\net10.0-windows\win-x64\DesktopPet.exe --selftest=resultado.txt --only=<o que você mudou>
```

O autoteste roda com o código de verdade; o código de saída é o número de falhas. Os detalhes estão em
[Para desenvolvedores](https://lfeliipeadv-stack.github.io/meu-pet/wiki/Para-desenvolvedores.html).

Ao contribuir, você concorda que a sua contribuição fica sob a [licença MIT](LICENSE.md) do projeto.
