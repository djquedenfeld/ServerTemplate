using System;
using System.IO;
using System.Text;
using System.Numerics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;

namespace ServerTest
{
    class WorldGenerator
    {
        readonly static string MAPPath = @"C:\Users\djque\Desktop\Visual Studio Projects\C#\ServerTest\ServerTest\MAP.bmp";
        //Ocean color used
        static Color ocean = Color.FromArgb(29, 162, 216);
        //Landmass color used
        static Color land = Color.FromArgb(126,200,80);

        public class Circle
        {
            public Circle parent;

            public Circle[] children;

            public List<Vector2> perimeter;
            public List<Vector2> area;

            public Vector2 center;

            public float resolutionFactor;
            public float radius;

            public Circle(Vector2 _center, float _radius, float _resolutionFactor)
            {
                perimeter = new List<Vector2>();
                area = new List<Vector2>();

                center = _center;
                radius = _radius;
                resolutionFactor = 1 / _resolutionFactor;

                #region Generate Perimeter
                for (float i = 0; i < 370; i+=resolutionFactor)
                {
                    Vector2 surfPt = new Vector2(
                        (int)(center.X + radius*MathF.Cos(i)), 
                        (int)(center.Y + radius*MathF.Sin(i)));
                    perimeter.Add(surfPt);
                }

                perimeter = RemoveDuplicates(perimeter);
                #endregion

                #region Generate Area
                Vector2 yBounds = GetYBounds(perimeter);
                int minY = (int)yBounds.X;
                int maxY = (int)yBounds.Y;

                List<Vector2> yLevel = new List<Vector2>();
                for (int i = minY; i < maxY; i++)
                {
                    yLevel.Clear();
                    foreach (Vector2 XV2 in perimeter)
                    {
                        if ((int)XV2.Y == i)
                        {
                            yLevel.Add(XV2);
                        }
                    }
                    if (yLevel.Count > 0)
                    {
                        Vector2 xBounds = GetXBounds(yLevel);
                        int minX = (int)xBounds.X;
                        int maxX = (int)xBounds.Y;
                        for (int j = minX; j < maxX; j++)
                        {
                            area.Add(new Vector2(j, i));
                        }
                    }
                }
                area = RemoveDuplicates(area);
                #endregion
            }
            public Circle(Vector2 _center, float _radius, float _resolutionFactor, Circle _parent, int numChildren)
            {
                perimeter = new List<Vector2>();
                area = new List<Vector2>();

                children = new Circle[numChildren];

                center = _center;
                radius = _radius;
                resolutionFactor = 1 / _resolutionFactor;
                parent = _parent;

                #region Generate Perimeter
                for (float i = 0; i < 370; i += resolutionFactor)
                {
                    Vector2 surfPt = new Vector2(
                        (int)(center.X + radius * MathF.Cos(i)),
                        (int)(center.Y + radius * MathF.Sin(i)));
                    perimeter.Add(surfPt);
                }

                perimeter = RemoveDuplicates(perimeter);
                #endregion

                #region Generate Area
                Vector2 yBounds = GetYBounds(perimeter);
                int minY = (int)yBounds.X;
                int maxY = (int)yBounds.Y;

                List<Vector2> yLevel = new List<Vector2>();
                for (int i = minY; i < maxY; i++)
                {
                    yLevel.Clear();
                    foreach (Vector2 XV2 in perimeter)
                    {
                        if ((int)XV2.Y == i)
                        {
                            yLevel.Add(XV2);
                        }
                    }
                    if (yLevel.Count > 0)
                    {
                        Vector2 xBounds = GetXBounds(yLevel);
                        int minX = (int)xBounds.X;
                        int maxX = (int)xBounds.Y;
                        for (int j = minX; j < maxX; j++)
                        {
                            area.Add(new Vector2(j, i));
                        }
                    }
                }
                area = RemoveDuplicates(area);
                #endregion
            }
            public List<Vector2> RemoveDuplicates(List<Vector2> original)
            {
                List<Vector2> newList = new List<Vector2>();
                try
                {
                    newList.Exists(query => query.Equals(original[0]));
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Exception thrown attempting to remove duplicates: {ex}");
                    return original;
                }
                foreach (Vector2 testObj in original)
                {
                    if (!newList.Exists(query => query.Equals(testObj)))
                    {
                        newList.Add(testObj);
                    }
                }
                return newList;
            }

            public void DrawPerimeter(Bitmap bmp, Color color)
            {
                foreach (Vector2 perimeter in perimeter)
                {
                    int currentX = (int)MathF.Max(perimeter.X, 0);
                    int currentY = (int)MathF.Max(perimeter.Y, 0);
                    bmp.SetPixel(currentX, currentY, color);
                }
            }

            public void DrawArea(Bitmap bmp, Color color)
            {
                foreach (Vector2 area in area)
                {
                    int currentX = (int)MathF.Max(area.X, 0);
                    int currentY = (int)MathF.Max(area.Y, 0);
                    bmp.SetPixel(currentX, currentY, color);
                }
            }

            public static void DiffCircles(Circle original, Circle toSubtract)
            {
                for (int i = original.area.Count-1; i > 0; i--)
                {
                    for (int j = toSubtract.area.Count-1; j > 0; j--)
                    {
                        if (original.area[i-1].X == toSubtract.area[j-1].X
                            && original.area[i-1].Y == toSubtract.area[j-1].Y)
                        {
                            original.area.Remove(original.area[i-1]);
                        }
                    }
                }
            }
        }

        /// <summary>GetYBounds takes a List<Vector2> range and returns a Vector2(min,max),
        /// the min and max of the Y values of that range</summary>
        /// <param name="range"></param>
        /// <returns>The range to determine the bounds for</returns>
        public static Vector2 GetYBounds(List<Vector2> range)
        {
            int max = (int)range[0].Y;
            int min = (int)range[0].Y;

            int newMax;
            int newMin;

            foreach (Vector2 v2 in range)
            {
                newMax = (int)v2.Y;
                newMin = (int)v2.Y;

                if (newMax > max)
                {
                    max = newMax;
                }
                if (newMin < min)
                {
                    min = newMin;
                }
            }
            return new Vector2(min, max);
        }

        /// <summary>GetXBounds takes a List<Vector2> range and returns a Vector2(min,max),
        /// the min and max of the X values of that range</summary>
        /// <param name="range"></param>
        /// <returns>The range to determine the bounds for</returns>
        public static Vector2 GetXBounds(List<Vector2> range)
        {
            int max = (int)range[0].X;
            int min = (int)range[0].X;

            int newMax;
            int newMin;

            foreach (Vector2 v2 in range)
            {
                newMax = (int)v2.X;
                newMin = (int)v2.X;

                if (newMax > max)
                {
                    max = newMax;
                }
                if (newMin < min)
                {
                    min = newMin;
                }
            }
            return new Vector2(min, max);
        }

        public static void DrawLine(List<Vector2> points, Bitmap bmp, Color color)
        {
            foreach (Vector2 point in points)
            {
                int currentX = (int)MathF.Max(point.X, 0);
                int currentY = (int)MathF.Max(point.Y, 0);
                bmp.SetPixel(currentX, currentY, color);
            }
        }

        public static void RenderBitmap(int numGenerations, int circlesPerGen)
        {
            Bitmap returnMap = new Bitmap(1360, 768);
            Random random = new Random();

            //Consider: building a list of "Generation" structs(int, Circle[])
            List<Circle[]> generations = new List<Circle[]>();

            #region Array Initialization
            for(int i = 0; i < numGenerations; i++)
            {
                Circle[] circles = new Circle[(int)Math.Max(MathF.Pow(circlesPerGen,i),1)];
                generations.Add(circles);
            }
            #endregion

            /*INITIALIZER STEP
            Starts by creating a blank map with an ocean color background.*/
            for (int i = 0; i<returnMap.Width; i++)
            {
                for(int j =0; j<returnMap.Height; j++)
                {
                    returnMap.SetPixel(i, j, ocean);
                }
            }

            /*GENERATE FIRST CIRCLE STEP
            Draw a circle to the map created
            A higher resolution factor increases the resolution of the map
            A low resolution may result in generation artefacts at large radii
            A higher resolution factor may *substantially* increase processing time.

            I found at a radius of 150 pixels and a resolution of 10, processing time was about 10 seconds.
            This, of course, will scale exponentially as each generation of circles pass.*/

            generations[0][0] = new Circle(new Vector2(400, 320), 150, 50, null, circlesPerGen);
            generations[0][0].parent = generations[0][0];

            /*GENERATION ITERATION STEP
            In this step we are going to iteratively draw circles on the surface of the preceding circle,
            then take the difference of the (i-1)th generation circle with the ith generation circle*/
            //TODO: Add multithreading to process faster

            for(int i = 0; i < numGenerations-1; i++)
            {
                //Primary for loop which selects which Generation (List<Circle[]>) we are selecting from
                //gPos holds the place of which a circle is added to the generation array, so that
                //each secondary for loop can appropriately add to the array. is reset in the primary loop
                int gPos = 0;

                for(int j = 0; j < generations[i].Length; j++)
                {
                    //Secondary for loop which selects the parent circle from the preceding generation
                    Circle precedingCircle = generations[i][j];

                    for (int k = 0; k < precedingCircle.children.Length; k++)
                    {
                        //Tertiary for loop which creates circles and assigns them to generation list arrays and
                        //parent's children array

                        //TODO: Fix generation i assigning all circles to generation i-1, rather than even distribution
                        List<Vector2> perimeterIntersection = new List<Vector2>();
                        foreach(Vector2 perim in precedingCircle.perimeter)
                        {
                            if(generations[0][0].area.Exists(query => query.Equals(perim)))
                            {
                                perimeterIntersection.Add(perim);
                            }
                        }

                        //Generate circle
                        Circle toAdd =
                            new Circle(
                                perimeterIntersection[random.Next(0, perimeterIntersection.Count-1)],
                                precedingCircle.radius/3,
                                1/precedingCircle.resolutionFactor,
                                precedingCircle,
                                circlesPerGen);

                        //Add circle to precedingCircle.children
                        precedingCircle.children[k] = toAdd;
                        generations[i+1][gPos++] = toAdd;

                        //toAdd.DrawPerimeter(returnMap, Color.White);
                        Console.WriteLine($"Iteration: i:{i}, j:{j}, k:{k}");
                    }
                }
            }
            for(int i = 0; i < generations.Count-1; i++)
            {
                for(int j = 0; j < generations[i].Length; j++)
                {
                    foreach (Circle circle in generations[i][j].children)
                    {
                        Circle.DiffCircles(generations[0][0], circle);
                    }
                }
            }
                //Circle G2 = new Circle(G1.perimeter[random.Next(0, G1.perimeter.Count)], G1.radius/2, 200);
                //Circle.DiffCircles(G1, G2);
            generations[0][0].DrawArea(returnMap, land);

            FileStream mapPath = File.Open(MAPPath, FileMode.OpenOrCreate);
            returnMap.Save(mapPath, ImageFormat.Bmp);
            mapPath.Close();
        }
    }
}
