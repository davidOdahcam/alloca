using System.Text.Json;
using System.Text.Json.Serialization;

namespace Alloca.IoC.Config;

/// <summary>
/// Garante que todo <see cref="DateTime"/> seja serializado em ISO 8601 com o sufixo 'Z' (UTC).
/// As datas são armazenadas em UTC no banco, mas o EF Core materializa valores
/// <c>datetime2</c> do SQL Server com <see cref="DateTimeKind.Unspecified"/>, fazendo o
/// <c>System.Text.Json</c> emitir a data sem o indicador de fuso (ex.: <c>2026-06-06T11:00:00</c>).
/// Sem o 'Z', o front-end interpreta a string como horário local, exibindo o horário errado.
/// Este conversor normaliza qualquer <see cref="DateTime"/> para UTC, tanto na leitura quanto na escrita.
/// </summary>
public sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var valor = reader.GetDateTime();
        return valor.Kind switch
        {
            DateTimeKind.Utc => valor,
            DateTimeKind.Local => valor.ToUniversalTime(),
            _ => DateTime.SpecifyKind(valor, DateTimeKind.Utc)
        };
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
        // WriteStringValue de um DateTime com Kind=Utc gera ISO 8601 com 'Z' automaticamente.
        writer.WriteStringValue(utc);
    }
}
