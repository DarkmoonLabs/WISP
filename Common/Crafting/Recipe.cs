using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    public class Recipe
    {
        public struct RecipeComponent
        {
            public Stat StatIngredient { get; set; }
            public string ItemIngredient { get; set; }
            public int Quantity { get; set; }
        }

        public Recipe()
        {
            Ingredients = new List<RecipeComponent>();
            Results = new List<RecipeComponent>();
        }

        public int ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string RecipeGroup { get; set; }
        public List<RecipeComponent> Ingredients { get; set; }
        public List<RecipeComponent> Results { get; set; }

    }
}
