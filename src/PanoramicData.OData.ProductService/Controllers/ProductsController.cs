﻿using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.OData;
using PanoramicData.OData.ProductService.Models;

namespace PanoramicData.OData.ProductService.Controllers
{
	/*
    To add a route for this controller, merge these statements into the Register method of the WebApiConfig class. Note that OData URLs are case sensitive.

    using System.Web.Http.OData.Builder;
    using ProductService.Models;
    ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
    builder.EntitySet<Product>("Products");
    config.Routes.MapODataRoute("odata", "odata", builder.GetEdmModel());
    */
	public class ProductsController : ODataController
	{
		private readonly ProductServiceContext db = new();


		// GET odata/Products
		[EnableQuery]
		public IQueryable<Product> GetProducts() => db.Products;

		// GET odata/Products(5)
		[EnableQuery]
		public SingleResult<Product> GetProduct([FromODataUri] int key) => SingleResult.Create(db.Products.Where(product => product.ID == key));

		// PUT odata/Products(5)
		public IHttpActionResult Put([FromODataUri] int key, Product product)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			if (key != product.ID)
			{
				return BadRequest();
			}

			db.Entry(product).State = EntityState.Modified;

			try
			{
				db.SaveChanges();
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!ProductExists(key))
				{
					return NotFound();
				}
				else
				{
					throw;
				}
			}

			return Updated(product);
		}

		// POST odata/Products
		public IHttpActionResult Post(Product product)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			db.Products.Add(product);
			db.SaveChanges();

			return Created(product);
		}

		// PATCH odata/Products(5)
		[AcceptVerbs("PATCH", "MERGE")]
		public IHttpActionResult Patch([FromODataUri] int key, Delta<Product> patch)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			var product = db.Products.Find(key);
			if (product == null)
			{
				return NotFound();
			}

			patch.Patch(product);

			try
			{
				db.SaveChanges();
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!ProductExists(key))
				{
					return NotFound();
				}
				else
				{
					throw;
				}
			}

			return Updated(product);
		}

		// DELETE odata/Products(5)
		public IHttpActionResult Delete([FromODataUri] int key)
		{
			var product = db.Products.Find(key);
			if (product == null)
			{
				return NotFound();
			}

			db.Products.Remove(product);
			db.SaveChanges();

			return StatusCode(HttpStatusCode.NoContent);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				db.Dispose();
			}
			base.Dispose(disposing);
		}

		private bool ProductExists(int key) => db.Products.Count(e => e.ID == key) > 0;
	}
}
