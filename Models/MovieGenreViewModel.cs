using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using MvcMovie.Models;
using MvcMovie.Utilities;

namespace MvcMovie.ViewModels
{
    public class MovieGenreViewModel
    {
        public PaginatedList<Movie>? Movies { get; set; }
        public SelectList? Genres { get; set; }
        public string? MovieGenre { get; set; }
        public string? SearchString { get; set; }
        public string? CurrentSort { get; set; }
        public int? PageNumber { get; set; }
        public string ResetFilters { get; set; }

    }

}