﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace StepsTakenOnScreen
{
    /// <summary>A snippet of formatted text.</summary>
    internal interface IFormattedText
    {
        /// <summary>The font color (or <c>null</c> for the default color).</summary>
        Color? Color { get; }

        /// <summary>The text to format.</summary>
        string Text { get; }

        /// <summary>Whether to draw bold text.</summary>
        bool Bold { get; }
    }
    
    /// <summary>A snippet of formatted text.</summary>
    internal struct FormattedText : IFormattedText
    {
        /********
        ** Accessors
        *********/
        /// <summary>The text to format.</summary>
        public string Text { get; }

        /// <summary>The font color (or <c>null</c> for the default color).</summary>
        public Color? Color { get; }

        /// <summary>Whether to draw bold text.</summary>
        public bool Bold { get; }


        /********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="text">The text to format.</param>
        /// <param name="color">The font color (or <c>null</c> for the default color).</param>
        /// <param name="bold">Whether to draw bold text.</param>
        public FormattedText(string text, Color? color = null, bool bold = false)
        {
            this.Text = text;
            this.Color = color;
            this.Bold = bold;
        }
    }

    /// <summary>Provides utility methods for drawing to the screen.</summary>
    internal static class DrawHelper
    {
        /****
       ** Fonts
       ****/
        /// <summary>Get the dimensions of a space character.</summary>
        /// <param name="font">The font to measure.</param>
        public static float GetSpaceWidth(SpriteFont font)
        {
            return font.MeasureString("A B").X - font.MeasureString("AB").X;
        }

        /****
        ** UI
        ****/
        /// <summary>Draw a pretty hover box for the given text.</summary>
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        /// <param name="label">The text to display.</param>
        /// <param name="position">The position at which to draw the text.</param>
        /// <param name="wrapWidth">The maximum width to display.</param>

        public static Vector2 DrawHoverBox(SpriteBatch spriteBatch, string label, in Vector2 position, float wrapWidth)
        {
            const int paddingSize = 27;
            const int gutterSize = 20;

            Vector2 labelSize = spriteBatch.DrawTextBlock(Game1.smallFont, label, position + new Vector2(gutterSize), wrapWidth); // draw text to get wrapped text dimensions
            StardewValley.Menus.IClickableMenu.drawTextureBox(spriteBatch, Game1.menuTexture, new Rectangle(0, 256, 60, 60), (int)position.X, (int)position.Y, (int)labelSize.X + paddingSize + gutterSize, (int)labelSize.Y + paddingSize, Color.White);
            spriteBatch.DrawTextBlock(Game1.smallFont, label, position + new Vector2(gutterSize), wrapWidth); // draw again over texture box

            return labelSize + new Vector2(paddingSize);
        }

        /*********
        ** Public methods
        *********/
        /****
        ** Drawing
        ****/
        /// <summary>Draw a block of text to the screen with the specified wrap width.</summary>
        /// <param name="batch">The sprite batch.</param>
        /// <param name="font">The sprite font.</param>
        /// <param name="text">The block of text to write.</param>
        /// <param name="position">The position at which to draw the text.</param>
        /// <param name="wrapWidth">The width at which to wrap the text.</param>
        /// <param name="color">The text color.</param>
        /// <param name="bold">Whether to draw bold text.</param>
        /// <param name="scale">The font scale.</param>
        /// <returns>Returns the text dimensions.</returns>
        public static Vector2 DrawTextBlock(this SpriteBatch batch, SpriteFont font, string text, Vector2 position, float wrapWidth, Color? color = null, bool bold = false, float scale = 1)
        {
            return batch.DrawTextBlock(font, new IFormattedText[] { new FormattedText(text, color, bold) }, position, wrapWidth, scale);
        }

        /// <summary>Draw a block of text to the screen with the specified wrap width.</summary>
        /// <param name="batch">The sprite batch.</param>
        /// <param name="font">The sprite font.</param>
        /// <param name="text">The block of text to write.</param>
        /// <param name="position">The position at which to draw the text.</param>
        /// <param name="wrapWidth">The width at which to wrap the text.</param>
        /// <param name="scale">The font scale.</param>
        /// <returns>Returns the text dimensions.</returns>
        public static Vector2 DrawTextBlock(this SpriteBatch batch, SpriteFont font, IEnumerable<IFormattedText> text, Vector2 position, float wrapWidth, float scale = 1)
        {
            if (text == null)
                return new Vector2(0, 0);

            // track draw values
            float xOffset = 0;
            float yOffset = 0;
            float lineHeight = font.MeasureString("ABC").Y * scale;
            float spaceWidth = DrawHelper.GetSpaceWidth(font) * scale;
            float blockWidth = 0;
            float blockHeight = lineHeight;

            // draw text snippets
            foreach (IFormattedText snippet in text)
            {
                if (snippet?.Text == null)
                    continue;

                // track surrounding spaces for combined translations
                bool startSpace = snippet.Text.StartsWith(" ");
                bool endSpace = snippet.Text.EndsWith(" ");

                // get word list
                IList<string> words = new List<string>();
                string[] rawWords = snippet.Text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0, last = rawWords.Length - 1; i <= last; i++)
                {
                    // get word
                    string word = rawWords[i];
                    if (startSpace && i == 0)
                        word = $" {word}";
                    if (endSpace && i == last)
                        word += " ";

                    // split on newlines
                    string wordPart = word;
                    int newlineIndex;
                    while ((newlineIndex = wordPart.IndexOf(Environment.NewLine, StringComparison.InvariantCulture)) >= 0)
                    {
                        if (newlineIndex == 0)
                        {
                            words.Add(Environment.NewLine);
                            wordPart = wordPart.Substring(Environment.NewLine.Length);
                        }
                        else if (newlineIndex > 0)
                        {
                            words.Add(wordPart.Substring(0, newlineIndex));
                            words.Add(Environment.NewLine);
                            wordPart = wordPart.Substring(newlineIndex + Environment.NewLine.Length);
                        }
                    }

                    // add remaining word (after newline split)
                    if (wordPart.Length > 0)
                        words.Add(wordPart);
                }

                // draw words to screen
                bool isFirstOfLine = true;
                foreach (string word in words)
                {
                    // check wrap width
                    float wordWidth = font.MeasureString(word).X * scale;
                    float prependSpace = isFirstOfLine ? 0 : spaceWidth;
                    if (word == Environment.NewLine || ((wordWidth + xOffset + prependSpace) > wrapWidth && (int)xOffset != 0))
                    {
                        xOffset = 0;
                        yOffset += lineHeight;
                        blockHeight += lineHeight;
                        isFirstOfLine = true;
                    }
                    if (word == Environment.NewLine)
                        continue;

                    // draw text
                    Vector2 wordPosition = new Vector2(position.X + xOffset + prependSpace, position.Y + yOffset);
                    if (snippet.Bold)
                        Utility.drawBoldText(batch, word, font, wordPosition, snippet.Color ?? Color.Black, scale);
                    else
                        batch.DrawString(font, word, wordPosition, snippet.Color ?? Color.Black, 0, Vector2.Zero, scale, SpriteEffects.None, 1);

                    // update draw values
                    if (xOffset + wordWidth + prependSpace > blockWidth)
                        blockWidth = xOffset + wordWidth + prependSpace;
                    xOffset += wordWidth + prependSpace;
                    isFirstOfLine = false;
                }
            }

            // return text position & dimensions
            return new Vector2(blockWidth, blockHeight);
        }
    }
}
