#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

public static class BackupProjetoLiga
{
    private class Fonte
    {
        public string pasta;
        public string entrada;

        public Fonte(string p, string e)
        {
            pasta = p;
            entrada = e;
        }
    }

    private class Arquivo
    {
        public string caminho;
        public string entrada;
        public long tamanho;
        public DateTime alterado;
    }

    [Serializable]
    private class ValorJson
    {
        public string value = "";
    }

    private static double proximoProgresso;
    private static bool executando;

    [MenuItem("Tools/Canal/Backup do projeto e liga")]
    private static void BackupBasico()
    {
        Executar(false);
    }

    [MenuItem("Tools/Canal/Backup incluindo Recordings")]
    private static void BackupComVideos()
    {
        Executar(true);
    }

    private static void Executar(bool incluirVideos)
    {
        if (executando) return;

        executando = true;

        string temporario = null;
        string destino = null;

        bool concluido = false;
        bool temDados = false;
        bool temMataMata = false;
        bool temVideos = false;

        int quantidade = 0;

        try
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling ||
                EditorApplication.isUpdating)
            {
                throw new InvalidOperationException(
                    "Pare o Recorder, saia do Play Mode e aguarde " +
                    "a compilação/importação.");
            }

            var prefab = PrefabStageUtility.GetCurrentPrefabStage();

            if (prefab != null && prefab.scene.isDirty)
            {
                throw new InvalidOperationException(
                    "Salve as alterações do prefab e saia do " +
                    "Prefab Mode antes do backup.");
            }

            if (!EditorSceneManager.SaveOpenScenes())
            {
                throw new InvalidOperationException(
                    "Não foi possível salvar todas as cenas.");
            }

            AssetDatabase.SaveAssets();

            string projeto =
                Directory.GetParent(Application.dataPath).FullName;

            string ligas =
                Path.Combine(Application.persistentDataPath, "Ligas");

            string mataMata =
                Path.Combine(Application.persistentDataPath, "MataMata");

            var fontes = new List<Fonte>();

            foreach (string nome in new[]
            {
                "Assets",
                "Packages",
                "ProjectSettings"
            })
            {
                string pasta = Path.Combine(projeto, nome);

                if (!Directory.Exists(pasta))
                {
                    throw new DirectoryNotFoundException(
                        "Pasta obrigatória ausente: " + pasta);
                }

                fontes.Add(new Fonte(pasta, "Projeto/" + nome));
            }

            if (!File.Exists(Path.Combine(
                    projeto, "Packages", "manifest.json")) ||
                !File.Exists(Path.Combine(
                    projeto, "ProjectSettings", "ProjectVersion.txt")))
            {
                throw new InvalidOperationException(
                    "Confira o manifesto e a versão deste projeto Unity.");
            }

            string preferencias =
                Path.Combine(projeto, "UserSettings");

            if (Directory.Exists(preferencias))
            {
                fontes.Add(new Fonte(
                    preferencias, "Projeto/UserSettings"));
            }

            temDados = Directory.Exists(ligas);

            if (temDados)
            {
                fontes.Add(new Fonte(
                    ligas, "DadosDasLigas/Ligas"));
            }

            temMataMata = Directory.Exists(mataMata);

            if (temMataMata)
            {
                fontes.Add(new Fonte(
                    mataMata, "DadosDoMataMata/MataMata"));
            }

            string gravacoes =
                Path.Combine(projeto, "Recordings");

            temVideos =
                incluirVideos && Directory.Exists(gravacoes);

            if (temVideos)
            {
                fontes.Add(new Fonte(
                    gravacoes, "Projeto/Recordings"));
            }

            ValidarPacotes(projeto, fontes);

            string nomeProduto = Application.productName;

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                nomeProduto = nomeProduto.Replace(c, '_');
            }

            if (string.IsNullOrWhiteSpace(nomeProduto))
            {
                nomeProduto = "Unity";
            }

            destino = EditorUtility.SaveFilePanel(
                "Salvar backup",
                EditorPrefs.GetString(
                    "Canal.Backup.Pasta",
                    Directory.GetParent(projeto)?.FullName ?? projeto),
                "BACKUP_" + nomeProduto + "_" +
                    DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"),
                "zip");

            if (string.IsNullOrEmpty(destino)) return;

            if (!destino.EndsWith(
                ".zip", StringComparison.OrdinalIgnoreCase))
            {
                destino += ".zip";
            }

            destino = Path.GetFullPath(destino);

            if (Dentro(destino, projeto) || Dentro(destino, ligas) ||
                Dentro(destino, mataMata))
            {
                throw new InvalidOperationException(
                    "Escolha uma pasta de destino fora do projeto " +
                    "e fora dos dados das ligas e do mata-mata.");
            }

            if (File.Exists(destino))
            {
                throw new IOException(
                    "Esse ZIP já existe. Escolha outro nome para " +
                    "preservar o backup anterior.");
            }

            proximoProgresso = 0;

            var arquivos = new List<Arquivo>();
            var pastas = new List<string>();

            foreach (Fonte fonte in fontes)
            {
                Listar(
                    fonte.pasta,
                    fonte.entrada,
                    arquivos,
                    pastas);
            }

            quantidade = arquivos.Count;

            long total = 0;

            foreach (Arquivo a in arquivos)
            {
                total += a.tamanho;
            }

            long feito = 0;

            temporario = Path.Combine(
                Path.GetDirectoryName(destino),
                ".unity_backup_" +
                Guid.NewGuid().ToString("N") +
                ".partial");

            var hashes =
                new Dictionary<string, string>(StringComparer.Ordinal);

            using (var saida = new FileStream(
                temporario,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None))
            using (var zip = new ZipArchive(
                saida,
                ZipArchiveMode.Create,
                false,
                Encoding.UTF8))
            {
                foreach (string pasta in pastas)
                {
                    zip.CreateEntry(pasta + "/");
                }

                foreach (Arquivo a in arquivos)
                {
                    ConferirArquivo(a);

                    Progresso(
                        "Copiando: " + a.entrada,
                        0.1f + 0.5f * feito / Math.Max(1f, total));

                    var entrada = zip.CreateEntry(
                        a.entrada,
                        System.IO.Compression.CompressionLevel.Fastest);

                    using (var origem = new FileStream(
                        a.caminho,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read))
                    using (var copia = entrada.Open())
                    {
                        hashes.Add(
                            a.entrada,
                            CopiarHash(origem, copia, n =>
                            {
                                feito += n;

                                Progresso(
                                    "Copiando: " + a.entrada,
                                    0.1f + 0.5f * feito /
                                    Math.Max(1f, total));
                            }));
                    }

                    ConferirArquivo(a);
                }

                AdicionarTexto(
                    zip,
                    hashes,
                    "LEIA-ME.txt",
                    Instrucoes(
                        projeto, fontes, temDados, temMataMata, temVideos));

                var manifesto = new StringBuilder();

                foreach (var h in hashes)
                {
                    manifesto.AppendLine(h.Value + "  " + h.Key);
                }

                AdicionarTexto(
                    zip,
                    hashes,
                    "SHA256SUMS.txt",
                    manifesto.ToString());
            }

            foreach (Arquivo a in arquivos)
            {
                ConferirArquivo(a);
            }

            using (var leitura = File.OpenRead(temporario))
            using (var zip = new ZipArchive(
                leitura,
                ZipArchiveMode.Read,
                false,
                Encoding.UTF8))
            {
                if (zip.Entries.Count !=
                    pastas.Count + arquivos.Count + 2)
                {
                    throw new InvalidDataException(
                        "A quantidade de itens do ZIP está incorreta.");
                }

                long totalVerificar = 0;

                foreach (var entrada in zip.Entries)
                {
                    totalVerificar += entrada.Length;
                }

                long verificado = 0;

                foreach (var entrada in zip.Entries)
                {
                    if (entrada.FullName.EndsWith(
                        "/", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    string esperado;

                    if (!hashes.TryGetValue(
                        entrada.FullName, out esperado))
                    {
                        throw new InvalidDataException(
                            "Item inesperado no ZIP: " +
                            entrada.FullName);
                    }

                    using (var conteudo = entrada.Open())
                    {
                        string atual = CopiarHash(
                            conteudo,
                            null,
                            n =>
                            {
                                verificado += n;

                                Progresso(
                                    "Verificando: " + entrada.FullName,
                                    0.6f + 0.39f * verificado /
                                    Math.Max(1f, totalVerificar));
                            });

                        if (atual != esperado)
                        {
                            throw new InvalidDataException(
                                "Falha de integridade: " +
                                entrada.FullName);
                        }
                    }
                }
            }

            Progresso("Concluindo backup...", 1f);

            File.Move(temporario, destino);

            temporario = null;
            concluido = true;
        }
        catch (OperationCanceledException)
        {
            Debug.Log(
                "Backup cancelado. Os arquivos originais " +
                "foram preservados.");
        }
        catch (Exception e)
        {
            Debug.LogError(
                "Backup não concluído: " + e.Message);

            EditorUtility.DisplayDialog(
                "Backup não concluído",
                e.Message,
                "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            executando = false;

            if (temporario != null && File.Exists(temporario))
            {
                try
                {
                    File.Delete(temporario);
                }
                catch
                {
                    Debug.LogWarning(
                        "Remova o arquivo temporário: " + temporario);
                }
            }
        }

        if (concluido)
        {
            try
            {
                EditorPrefs.SetString(
                    "Canal.Backup.Pasta",
                    Path.GetDirectoryName(destino));
            }
            catch (Exception e)
            {
                Debug.LogWarning(
                    "Backup salvo; não foi possível lembrar a pasta: " +
                    e.Message);
            }

            string mensagem =
                quantidade + " arquivos de origem copiados.\n\n" +
                "Projeto: incluído.\n" +
                "Dados das ligas: " +
                (temDados
                    ? "incluídos."
                    : "não encontrados neste projeto.") +
                "\nDados do mata-mata: " +
                (temMataMata
                    ? "incluídos."
                    : "não encontrados neste projeto.") +
                "\nRecordings: " +
                (temVideos ? "incluída." : "não incluída.") +
                "\n\nZIP verificado:\n" + destino;

            Debug.Log(mensagem);

            EditorUtility.DisplayDialog(
                "Backup concluído",
                mensagem,
                "OK");

            try
            {
                EditorUtility.RevealInFinder(destino);
            }
            catch (Exception e)
            {
                Debug.LogWarning(
                    "O ZIP foi salvo, mas a pasta não abriu: " +
                    e.Message);
            }
        }
    }

    private static void Listar(
        string pasta,
        string entrada,
        List<Arquivo> arquivos,
        List<string> pastas)
    {
        SemLinks(pasta);
        Progresso("Listando: " + entrada, 0f);

        pastas.Add(entrada);

        foreach (string arquivo in Directory.GetFiles(pasta))
        {
            SemLinks(arquivo);

            var info = new FileInfo(arquivo);

            arquivos.Add(new Arquivo
            {
                caminho = arquivo,
                entrada = entrada + "/" + info.Name,
                tamanho = info.Length,
                alterado = info.LastWriteTimeUtc
            });

            Progresso("Listando: " + info.Name, 0f);
        }

        foreach (string subpasta in Directory.GetDirectories(pasta))
        {
            Listar(
                subpasta,
                entrada + "/" + Path.GetFileName(subpasta),
                arquivos,
                pastas);
        }
    }

    private static void SemLinks(string caminho)
    {
        if ((File.GetAttributes(caminho) &
            FileAttributes.ReparsePoint) != 0)
        {
            throw new IOException(
                "Foi encontrado um link simbólico/junção: " +
                caminho +
                ". Resolva essa referência antes de criar " +
                "um backup portátil.");
        }
    }

    private static void ConferirArquivo(Arquivo a)
    {
        var info = new FileInfo(a.caminho);

        if (!info.Exists ||
            info.Length != a.tamanho ||
            info.LastWriteTimeUtc != a.alterado)
        {
            throw new IOException(
                "O arquivo mudou durante o backup: " +
                a.caminho +
                ". Aguarde os programas terminarem de salvar " +
                "e tente novamente.");
        }
    }

    private static bool Dentro(string caminho, string pasta)
    {
        string a = Path.GetFullPath(caminho).TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar);

        string b = Path.GetFullPath(pasta).TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar);

        return string.Equals(
            a, b, StringComparison.OrdinalIgnoreCase) ||
            a.StartsWith(
                b + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidarPacotes(
        string projeto,
        List<Fonte> fontes)
    {
        string manifesto = File.ReadAllText(
            Path.Combine(projeto, "Packages", "manifest.json"));

        if (Regex.IsMatch(
            manifesto, @"file:(?:[A-Za-z]:|/|\\\\)"))
        {
            throw new InvalidOperationException(
                "Há uma dependência local com caminho absoluto " +
                "em Packages/manifest.json. Coloque o pacote " +
                "dentro do projeto e use um caminho relativo " +
                "antes do backup portátil.");
        }

        var pacotes =
            UnityEditor.PackageManager.PackageInfo
                .GetAllRegisteredPackages();

        if (pacotes == null || pacotes.Length == 0)
        {
            throw new InvalidOperationException(
                "Aguarde o Package Manager terminar " +
                "de carregar os pacotes.");
        }

        foreach (var pacote in pacotes)
        {
            string tipo = pacote.source.ToString();

            if (tipo != "Local" && tipo != "LocalTarball")
            {
                continue;
            }

            string origem = pacote.resolvedPath;

            if (tipo == "LocalTarball")
            {
                string padrao =
                    "\"" + Regex.Escape(pacote.name) + "\"" +
                    @"\s*:\s*(""(?:\\.|[^""\\])*"")";

                Match resultado = Regex.Match(manifesto, padrao);

                if (!resultado.Success)
                {
                    throw new InvalidOperationException(
                        "Não foi possível localizar o arquivo " +
                        "original do pacote: " + pacote.name);
                }

                string valor = JsonUtility.FromJson<ValorJson>(
                    "{\"value\":" +
                    resultado.Groups[1].Value +
                    "}").value;

                if (!valor.StartsWith(
                    "file:", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Confira o caminho do pacote local: " +
                        pacote.name);
                }

                origem = Path.GetFullPath(Path.Combine(
                    projeto,
                    "Packages",
                    Uri.UnescapeDataString(valor.Substring(5))));

                if (!File.Exists(origem))
                {
                    throw new FileNotFoundException(
                        "Arquivo original do pacote local " +
                        "não encontrado.",
                        origem);
                }
            }

            bool incluido = false;

            if (!string.IsNullOrEmpty(origem))
            {
                foreach (Fonte fonte in fontes)
                {
                    if (Dentro(origem, fonte.pasta))
                    {
                        incluido = true;
                    }
                }
            }

            if (!incluido)
            {
                throw new InvalidOperationException(
                    "O pacote local '" + pacote.name +
                    "' está fora das pastas incluídas. " +
                    "Incorpore-o ao projeto antes de criar " +
                    "um backup portátil.");
            }
        }
    }

    private static string CopiarHash(
        Stream origem,
        Stream destino,
        Action<int> avancar)
    {
        byte[] buffer = new byte[131072];

        using (var hash = SHA256.Create())
        {
            int lidos;

            while ((lidos = origem.Read(
                buffer, 0, buffer.Length)) > 0)
            {
                if (destino != null)
                {
                    destino.Write(buffer, 0, lidos);
                }

                hash.TransformBlock(
                    buffer, 0, lidos, buffer, 0);

                avancar(lidos);
            }

            hash.TransformFinalBlock(new byte[0], 0, 0);

            return BitConverter.ToString(hash.Hash)
                .Replace("-", "");
        }
    }

    private static void AdicionarTexto(
        ZipArchive zip,
        Dictionary<string, string> hashes,
        string nome,
        string texto)
    {
        byte[] conteudo =
            new UTF8Encoding(false).GetBytes(texto);

        using (var entrada = zip.CreateEntry(
            nome,
            System.IO.Compression.CompressionLevel.Fastest).Open())
        {
            entrada.Write(conteudo, 0, conteudo.Length);
        }

        using (var hash = SHA256.Create())
        {
            hashes.Add(
                nome,
                BitConverter.ToString(
                    hash.ComputeHash(conteudo)).Replace("-", ""));
        }
    }

    private static string Instrucoes(
        string projeto,
        List<Fonte> fontes,
        bool temDados,
        bool temMataMata,
        bool temVideos)
    {
        var texto = new StringBuilder();

        texto.AppendLine("BACKUP DO PROJETO UNITY, DAS LIGAS E DO MATA-MATA");
        texto.AppendLine(
            "Criado em: " +
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        texto.AppendLine("Unity: " + Application.unityVersion);
        texto.AppendLine("Company Name: " + Application.companyName);
        texto.AppendLine("Product Name: " + Application.productName);
        texto.AppendLine("Projeto de origem: " + projeto);
        texto.AppendLine(
            "Pasta de dados original: " +
            Application.persistentDataPath);
        texto.AppendLine();

        texto.AppendLine("PASTAS INCLUIDAS:");

        foreach (Fonte fonte in fontes)
        {
            texto.AppendLine("- " + fonte.entrada);
        }

        texto.AppendLine(
            "Dados de liga encontrados: " + temDados);
        texto.AppendLine(
            "Dados de mata-mata encontrados: " + temMataMata);
        texto.AppendLine(
            "Pasta Recordings incluida: " + temVideos);
        texto.AppendLine();

        texto.AppendLine("RESTAURACAO:");
        texto.AppendLine("1. Extraia o ZIP numa pasta local.");
        texto.AppendLine(
            "2. No Unity Hub, adicione a pasta Projeto.");
        texto.AppendLine(
            "3. Instale a mesma versao do Unity registrada acima.");
        texto.AppendLine(
            "4. O Unity reconstruira Library e pode baixar " +
            "pacotes pela internet.");
        texto.AppendLine(
            "5. Sem Play Mode, use LigaCampeonato > Liga > " +
            "Abrir pasta da temporada.");
        texto.AppendLine(
            "6. Feche o Unity. Copie para essa pasta o conteudo " +
            "da temporada correspondente");
        texto.AppendLine(
            "   que esta em DadosDasLigas/Ligas/<liga>/<temporada>.");
        texto.AppendLine(
            "7. Para restaurar o mata-mata, com o Unity fechado, copie " +
            "a pasta MataMata");
        texto.AppendLine(
            "   de DadosDoMataMata/MataMata para Application.persistentDataPath " +
            "do projeto restaurado,");
        texto.AppendLine(
            "   preservando MataMata/<competicao>/<edicao> e todos os arquivos.");
        texto.AppendLine(
            "8. Reabra o projeto e confira calendario, IDs " +
            "e configuracoes do Recorder.");
        texto.AppendLine();

        texto.AppendLine(
            "Assets e seus arquivos .meta foram preservados.");
        texto.AppendLine(
            "Library, Temp, Logs, obj, builds e arquivos externos " +
            "nao fazem parte desta selecao.");
        texto.AppendLine(
            "Recordings so entra quando existe na raiz do projeto " +
            "e foi escolhida a opcao com videos.");
        texto.AppendLine(
            "Projetos AE, videos em outros caminhos e preferencias " +
            "globais do Editor/Recorder nao entram.");
        texto.AppendLine(
            "Salve presets do Recorder dentro de Assets quando " +
            "disponivel; guarde tambem suas configuracoes.");
        texto.AppendLine(
            "Em pastas sincronizadas, aguarde o aplicativo " +
            "de nuvem concluir o envio.");
        texto.AppendLine(
            "SHA256SUMS.txt lista os hashes dos arquivos; " +
            "o ZIP foi reaberto e verificado na criacao.");

        return texto.ToString();
    }

    private static void Progresso(string mensagem, float valor)
    {
        if (EditorApplication.timeSinceStartup < proximoProgresso)
        {
            return;
        }

        proximoProgresso =
            EditorApplication.timeSinceStartup + 0.1;

        if (EditorUtility.DisplayCancelableProgressBar(
            "Backup do canal",
            mensagem,
            Mathf.Clamp01(valor)))
        {
            throw new OperationCanceledException();
        }
    }
}
#endif