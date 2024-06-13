namespace VideoSearch.Translator.Models;

public record TranslateRequest(string Q, string Source, string Target, string Format, int Alternatives);