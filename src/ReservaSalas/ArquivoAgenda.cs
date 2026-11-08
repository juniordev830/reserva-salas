using System.Text.Json;

namespace ReservaSalas;

/// <summary>
/// Persist agenda snapshots in a single local JSON document.
/// An atomic replace reduces the chance of a partial file after interruption.
/// For local demos only: this is not an encrypted, multiuser database.
/// </summary>
public static class ArquivoAgenda
{
    private const int LimiteBytes = 1_000_000;
    private sealed record Documento(
        int Versao, RecursoInfo[] Recursos, ReservaDetalhada[] Reservas);

    private static readonly JsonSerializerOptions Opcoes = new()
    {
        PropertyNameCaseInsensitive = false,
        WriteIndented = true,
    };

    public static AgendaSalas Carregar(string caminho)
    {
        ValidarCaminho(caminho);
        if (!File.Exists(caminho)) return new AgendaSalas();
        var info = new FileInfo(caminho);
        if (info.Length > LimiteBytes)
            throw new InvalidDataException("O arquivo ultrapassa 1 MB.");

        Documento? documento;
        try
        {
            documento = JsonSerializer.Deserialize<Documento>(
                File.ReadAllText(caminho), Opcoes);
        }
        catch (JsonException error)
        {
            throw new InvalidDataException("O arquivo contém JSON inválido.", error);
        }
        if (documento is null || documento.Versao != 1 ||
            documento.Recursos is null || documento.Reservas is null)
            throw new InvalidDataException("Versão ou estrutura inválida.");
        try
        {
            return AgendaSalas.Restaurar(documento.Recursos, documento.Reservas);
        }
        catch (Exception error) when (
            error is ArgumentException or InvalidOperationException
            or KeyNotFoundException or OverflowException)
        {
            throw new InvalidDataException("O arquivo contém reservas inválidas.", error);
        }
    }

    public static void Salvar(AgendaSalas agenda, string caminho)
    {
        ArgumentNullException.ThrowIfNull(agenda);
        ValidarCaminho(caminho);
        var documento = new Documento(1,
            agenda.Recursos().ToArray(), agenda.Reservas().ToArray());
        var bytes = JsonSerializer.SerializeToUtf8Bytes(documento, Opcoes);
        if (bytes.Length > LimiteBytes)
            throw new InvalidDataException("O arquivo ultrapassa 1 MB.");

        var pasta = Path.GetDirectoryName(Path.GetFullPath(caminho))!;
        Directory.CreateDirectory(pasta);
        var temporario = Path.Combine(pasta,
            ".agenda-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            using (var arquivo = new FileStream(temporario, FileMode.CreateNew,
                FileAccess.Write, FileShare.None))
            {
                arquivo.Write(bytes);
                arquivo.Flush(flushToDisk: true);
            }
            File.Move(temporario, caminho, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporario)) File.Delete(temporario);
        }
    }

    public static string CaminhoPadrao()
    {
        var customizado = Environment.GetEnvironmentVariable("GX_AGENDA_FILE");
        if (!string.IsNullOrWhiteSpace(customizado)) return customizado;
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".gx-reserva-salas", "agenda.json");
    }

    private static void ValidarCaminho(string caminho)
    {
        if (string.IsNullOrWhiteSpace(caminho))
            throw new ArgumentException("O caminho não pode ser vazio.", nameof(caminho));
    }
}
