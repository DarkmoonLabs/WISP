using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.XPath;

namespace Shared
{
    public class Crafting
    {
        public static Dictionary<int, Recipe> Recipes = new Dictionary<int, Recipe>();

        public static int LoadRecipes()
        {
            try
            {
                XPathNavigator nav = null;
                XPathDocument docNav = null;
                XPathNodeIterator NodeIter = null;
                String strExpression = null;

                // Open the XML.
                docNav = XMLHelper.LoadDocument(Environment.CurrentDirectory + "\\Config\\Recipes.xml", true);
                
                // Create a navigator to query with XPath.
                nav = docNav.CreateNavigator();
                strExpression = "//Recipes/Recipe";
                NodeIter = nav.Select(strExpression);

                int numLoaded = 0;

                //Iterate through the results showing the element value.
                while (NodeIter.MoveNext())
                {
                    Recipe r = new Recipe();
                    r.ID = int.Parse(NodeIter.Current.SelectSingleNode("ID").Value);
                    r.Name = NodeIter.Current.SelectSingleNode("Name").Value;
                    r.Description = NodeIter.Current.SelectSingleNode("Description").Value;
                    r.RecipeGroup = NodeIter.Current.SelectSingleNode("RecipeGroup").Value;
                    r.Description = NodeIter.Current.SelectSingleNode("Description").Value;                                        

                    XPathNodeIterator ingIt = NodeIter.Current.Select("./Ingredients/Ingredient");
                    GameEventType[] listeners = new GameEventType[ingIt.Count];
                    while (ingIt.MoveNext())
                    {
                        Recipe.RecipeComponent ing = new Recipe.RecipeComponent();
                        string resourceType = ingIt.Current.GetAttribute("resourceType", "");
                        if (resourceType.ToLower() == "item")
                        {
                            ing.ItemIngredient = ingIt.Current.GetAttribute("resource", "");
                        }
                        else
                        {
                            string statName = ingIt.Current.GetAttribute("resource", "");
                            Stat s = StatManager.Instance.AllStats.GetStat(statName);
                            ing.StatIngredient = s;
                        }

                        string amount = ingIt.Current.GetAttribute("amount", "");
                        if (amount.Length < 1)
                        {
                            amount = "1";
                        }
                        ing.Quantity = int.Parse(amount);
                        r.Ingredients.Add(ing);
                    }

                    ingIt = NodeIter.Current.Select("./Results/Result");
                    while (ingIt.MoveNext())
                    {
                        Recipe.RecipeComponent ing = new Recipe.RecipeComponent();
                        string resourceType = ingIt.Current.GetAttribute("resourceType", "");
                        if (resourceType.ToLower() == "item")
                        {
                            ing.ItemIngredient = ingIt.Current.GetAttribute("resource", "");
                        }
                        else
                        {
                            string statName = ingIt.Current.GetAttribute("resource", "");
                            Stat s = StatManager.Instance.AllStats.GetStat(statName);
                            if (s != null)
                            {
                                ing.StatIngredient = s.Copy();
                            }
                        }

                        string amount = ingIt.Current.GetAttribute("amount", "");
                        if (amount.Length < 1)
                        {
                            amount = "1";
                        }
                        
                        ing.Quantity = int.Parse(amount);
                        r.Results.Add(ing);
                    }

                    //Console.WriteLine("Loaded Recipe data for " + r.Name);
                    numLoaded++;
                    Recipes.Remove(r.ID);
                    Recipes.Add(r.ID, r);
                }

                return numLoaded;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Error loading Recipes: " + e.Message);
            }

            return -1;
        }

    }
}
