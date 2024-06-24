using System.ComponentModel.DataAnnotations.Schema;
using Pgvector;

namespace VideoSearch.Database.Models;

public class WordVector
{
    public string Word { get; set; }

    [Column(TypeName = "vector(300)")]
    public Vector Vector { get; set; }
}