using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MvcMovie.ViewModels;
using MvcMovie.Data;
using MvcMovie.Models;
using MvcMovie.Utilities;
using System.Diagnostics;
using Microsoft.AspNetCore.Http; //added for using sessions 

namespace MvcMovie.Controllers
{
    public class MoviesController : Controller
    {
        private readonly MvcMovieContext _context;
        private string ResetFilters;

        public MoviesController(MvcMovieContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string movieGenre, string searchString, string sortOrder, int? pageNumber)
        {
            if (!string.IsNullOrEmpty(ResetFilters))
            {
                // Clear session variables
                HttpContext.Session.Remove("MovieGenre");
                HttpContext.Session.Remove("SearchString");

                return RedirectToAction("Index");
            }

            if (string.IsNullOrEmpty(HttpContext.Session.GetString("MovieGenre")))
            {
              HttpContext.Session.SetString("MovieGenre", "");
                HttpContext.Session.SetString("SearchString", "");
            }

            string sessionMovieGenre = HttpContext.Session.GetString("MovieGenre");
            string sessionSearchString = HttpContext.Session.GetString("SearchString");

            if (!string.IsNullOrEmpty(movieGenre))
            {
                HttpContext.Session.SetString("MovieGenre", movieGenre);
            }
            else
            {
                movieGenre = sessionMovieGenre;
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                HttpContext.Session.SetString("SearchString", searchString);
            }
            else
            {
                searchString = sessionSearchString;
            }

            IQueryable<string> genreQuery = from m in _context.Movie
                                            orderby m.Genre
                                            select m.Genre;
            var movies = from m in _context.Movie
                         select m;

            if (!string.IsNullOrEmpty(searchString))
            {
                movies = movies.Where(s => s.Title.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(movieGenre))
            {
                movies = movies.Where(x => x.Genre == movieGenre);
            }

            ViewBag.TitleSortParam = sortOrder == "Title" ? "title_desc" : "Title";
            ViewBag.DateSortParam = sortOrder == "ReleaseDate" ? "releaseDate_desc" : "ReleaseDate";
            ViewBag.GenreSortParam = sortOrder == "Genre" ? "genre_desc" : "Genre";
            ViewBag.PriceSortParam = sortOrder == "Price" ? "price_desc" : "Price";
            ViewBag.RatingSortParam = sortOrder == "Rating" ? "rating_desc" : "Rating";

            // Sorting
            switch (sortOrder)
            {
                case "Title":
                    movies = movies.OrderBy(m => m.Title);
                    break;
                case "title_desc":
                    movies = movies.OrderByDescending(m => m.Title);
                    break;
                case "ReleaseDate":
                    movies = movies.OrderBy(m => m.ReleaseDate);
                    break;
                case "releaseDate_desc":
                    movies = movies.OrderByDescending(m => m.ReleaseDate);
                    break;
                case "Genre":
                    movies = movies.OrderBy(m => m.Genre);
                    break;
                case "Genre_Desc":
                    movies = movies.OrderByDescending(m => m.Genre);
                    break;
                case "Price":
                    movies = movies.OrderBy(m => m.Price);
                    break;
                case "Price_Desc":
                    movies = movies.OrderByDescending(m => m.Price);
                    break;
                case "Rating":
                    movies = movies.OrderBy(m => m.Rating);
                    break;
                case "Rating_Desc":
                    movies = movies.OrderByDescending(m => m.Rating);
                    break;
            }

            int pageSize = 5;
            var count = await movies.CountAsync();
            var paginatedMovies = await movies.Skip(((pageNumber ?? 1) - 1) * pageSize)
                                             .Take(pageSize)
                                             .ToListAsync();

            var movieGenreVM = new MovieGenreViewModel
            {
                Genres = new SelectList(await _context.Movie.Select(m => m.Genre).Distinct().ToListAsync()),
                Movies = new PaginatedList<Movie>(paginatedMovies, count, pageNumber ?? 1, pageSize),
                MovieGenre = movieGenre,
                SearchString = searchString,
                CurrentSort = sortOrder
            };

            return View(movieGenreVM);
        }


        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Movie == null)
            {
                return NotFound();
            }

            var movie = await _context.Movie
                .FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null)
            {
                return NotFound();
            }
            ViewData["EditMode"] = false;
            ViewData["DetailsMode"] = true;
            return PartialView("movieFields", movie);
        }

        // GET: Movies/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Movies/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> Create([Bind("Id,Title,ReleaseDate,Genre,Price,Rating")] Movie movie)
        {
            if (ModelState.IsValid)
            {
                if (MovieExists(movie.Title))
                {
                    return StatusCode(500, new { error = "Movie already exists" });
                }
                _context.Add(movie);
                await _context.SaveChangesAsync();
                    return Ok(); 
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") // Check if it's an AJAX request
            {
                return BadRequest(ModelState); // Return a BadRequest response with ModelState errors for AJAX requests
            }
            else
            {
                return View(movie); 
            }
        }




        // GET: Movies/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Movie == null)
            {
                return NotFound();
            }

            var movie = await _context.Movie.FindAsync(id);
            if (movie == null)
            {
                return NotFound();
            }
            ViewData["EditMode"] = true;
            ViewData["DetailsMode"] = false;

            return PartialView("movieFields", movie);
        }

        // POST: Movies/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
           public async Task<IActionResult> Edit(int id, MovieEditViewModel viewModel)
           {
           
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var movie = await _context.Movie.FindAsync(id);
                    if (movie == null)
                    {
                        return NotFound();
                    }

                
                    movie.Title = viewModel.Title;
                    movie.ReleaseDate = viewModel.ReleaseDate;
                    movie.Genre = viewModel.Genre;
                    movie.Price = viewModel.Price;
                    movie.Rating = viewModel.Rating;

                    _context.Update(movie);
                    await _context.SaveChangesAsync();
                    return Ok();
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }
            }
            return BadRequest(ModelState);
        }

        private bool MovieExists(int id)
        {
            throw new NotImplementedException();
        }

        // GET: Movies/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Movie == null)
            {
                return NotFound();
            }

            var movie = await _context.Movie
                .FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null)
            {
                return NotFound();
            }

            return View(movie);
        }

        // POST: Movies/Delete/5
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
         
            if (_context.Movie == null)
            {
                return Problem("Entity set 'MvcMovieContext.Movie'  is null.");
            }
            var movie = await _context.Movie.FindAsync(id);
            if (movie != null)
            {
                _context.Movie.Remove(movie);
            }

            await _context.SaveChangesAsync();
            
            return RedirectToAction(nameof(Index));
        }

        private bool MovieExists(string title)
        {
            return _context.Movie.Any(e => e.Title == title);
        }
    }
}
