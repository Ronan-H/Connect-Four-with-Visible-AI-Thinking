﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Connect_Four_with_Visible_AI_Thinking
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        /* Represents the board state (state of the chips)
         * 0 -> blank (no chip)
         * 1 -> red chip
         * 2 -> yellow chip
         */
        int[,] _board = new int[6, 7];
        Chip[,] _chips = new Chip[6, 7];
        Color[] chipColors = {Colors.White, Colors.Red, Colors.Yellow};

        // 0 = game finished
        // 1 = user's turn
        // 2 = AI's turn
        int _turn = 1;
        int _searchDepth = 3;
        int _aiSleepDelay = 10;
        bool _showAIThinking = true;
        bool _showAIColVals = true;
        int _bestMove = -1;
        TextBlock _prevBestVal = null;
        int _lowestColVal;
        int _highestColVal;

        public MainPage()
        {
            this.InitializeComponent();
        }

        bool setupDone = false;
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // setup the elements

            // scale the board correctly
            Loaded += delegate
            {
                if (!setupDone)
                {
                    boardGrid.Width = (rootPanel.ActualHeight / 6) * 7;
                    settingsPanel.Width = Math.Max(rootPanel.ActualWidth - boardGrid.Width, 0);

                    int borderSize = (int)(boardGrid.Width / 7) - 2;

                    for (int i = 0; i < 6; ++i)
                    {
                        boardGrid.RowDefinitions.Add(new RowDefinition());
                    }

                    for (int i = 0; i < 7; ++i)
                    {
                        boardGrid.ColumnDefinitions.Add(new ColumnDefinition());
                    }

                    // load the chips
                    for (int i = 0; i < 6; ++i)
                    {
                        for (int j = 0; j < 7; ++j)
                        {
                            Border border = new Border();
                            border.Width = borderSize;
                            border.Height = borderSize;
                            border.HorizontalAlignment = HorizontalAlignment.Center;
                            border.VerticalAlignment = VerticalAlignment.Center;
                            border.Background = new SolidColorBrush(Colors.Blue);

                            Ellipse chip = new Ellipse();
                            Binding binding = new Binding();
                            _chips[i, j] = new Chip(new SolidColorBrush(chipColors[0]));
                            binding.Source = _chips[i, j];
                            binding.Path = new PropertyPath("ChipChanged");
                            binding.Mode = BindingMode.OneWay;
                            BindingOperations.SetBinding(chip, Ellipse.FillProperty, binding);
                            chip.Width = (int)(borderSize * 0.8);
                            chip.Height = chip.Width;

                            _board[i, j] = 0;

                            border.SetValue(Grid.RowProperty, i);
                            border.SetValue(Grid.ColumnProperty, j);

                            chip.SetValue(Grid.RowProperty, i);
                            chip.SetValue(Grid.ColumnProperty, j);

                            boardGrid.Children.Add(border);
                            boardGrid.Children.Add(chip);
                        }
                    }

                    
                    setupDone = true;
                    updateBoard();
                }
            };
        }

        private async void OnBoardTapped(object sender, TappedRoutedEventArgs e)
        {
            if (_turn == 1 && !isGameWon())
            {
                int column = (int)(e.GetPosition((UIElement)sender).X / (boardGrid.Width / 7));

                if (!isColumnFull(column))
                {
                    placeChip(1, column);
                    await updateBoard();

                    if (isGameWon())
                    {
                        StatusText2.Foreground = new SolidColorBrush(Colors.White);
                        StatusText2.Text = "You Won!";
                    }
                    else
                    {
                        StatusText2.Foreground = new SolidColorBrush(Colors.Yellow);
                        StatusText2.Text = "AI is thinking...";

                        _turn = 2;
                        await Task.Run(() => doAiMove());
                        await updateBoard();

                        if (isGameWon())
                        {
                            StatusText2.Foreground = new SolidColorBrush(Colors.White);
                            StatusText2.Text = "You Lost!";
                        }
                        else
                        {
                            StatusText2.Foreground = new SolidColorBrush(Colors.Red);
                            StatusText2.Text = "Your turn";
                        }
                    }
                }
            }
        }

        private bool isColumnFull(int column)
        {
            return _board[0, column] != 0;
        }

        private bool isBoardFull()
        {
            for (int i = 0; i < 7; ++i)
            {
                if (!isColumnFull(i)) return false;
            }

            return true;
        }

        private void placeChip(int player, int column)
        {
            for (int i = 5; i >= 0; --i)
            {
                if (_board[i, column] == 0)
                {
                    _board[i, column] = player;
                    return;
                }
            }
        }


        // Remove the top most chip from a column.;
        private void removeChip(int column)
        {
            for (int i = 0; i < 6; ++i)
            {
                if (_board[i, column] != 0)
                {
                    _board[i, column] = 0;
                    return;
                }
            }
        }
        
        

        public async Task updateBoard()
        {
            Debug.WriteLine("Start of update");
            for (int i = 0; i < 6; ++i)
            {
                for (int j = 0; j < 7; ++j)
                {
                    Color chipColor = chipColors[_board[i, j]];
                    
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        if ((_chips[i, j]._color).Color.Equals(chipColor) == false)
                        {
                            _chips[i, j].ChipChanged = new SolidColorBrush(chipColor);

                            // assume only 1 chip changed
                            return;
                        }
                    });
                    
                }
            }


            Debug.WriteLine("End of update");
        }

        private async Task doAiMove()
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // disallow the user from clearing the board while minmax is running
                RestartButton.IsEnabled = false;
                ShowThinkingCheckbox.IsEnabled = false;

                for (int i = 0; i < boardGrid.Children.Count; ++i)
                {
                    if (boardGrid.Children.ElementAt(i) is TextBlock)
                    {
                        boardGrid.Children.RemoveAt(i);
                        i = 0;
                    }
                }
            });

            _lowestColVal = Int32.MaxValue;
            _highestColVal = Int32.MinValue;
            minMax(true, _searchDepth, true);

            if (_searchDepth != 0 && _lowestColVal == _highestColVal && (_lowestColVal == Int32.MaxValue || _lowestColVal == Int32.MinValue))
            {
                // All columns have value of +inf or -inf
                /* Meaning with optimal play, a player is guaranteed a win
                 * within <depth> moves. The AI will pick the last column.
                 * This means it won't bother winning straight away if it's guaranteed
                 * a win, and it won't bother trying to stop a player who is guaranteed a win
                 * with optimal play.
                 * 
                 * Solution: redo minmax with depth of 1. Does not work every time.
                 */

                Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    for (int i = 0; i < boardGrid.Children.Count; ++i)
                    {
                        if (boardGrid.Children.ElementAt(i) is TextBlock)
                        {
                            boardGrid.Children.RemoveAt(i);
                            i = 0;
                        }
                    }

                    StatusText2.Foreground = new SolidColorBrush(Colors.Red);
                    StatusText2.Text = "Your turn";
                }).AsTask().Wait();

                minMax(true, 1, true);
            }

            placeChip(2, _bestMove);
            _turn = 1;

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                RestartButton.IsEnabled = true;
                ShowThinkingCheckbox.IsEnabled = true;
            });
        }

        /* Finds how good the board is for the AI.
         * This is how terminal MinMax nodes are evaluated.
         * A value of >0 indicates the AI is winning.
         * A value of <0 indicates the player is winning.
         * A value of 0 indicates a tied board state.
         */
        private int getBoardEvaluation()
        {
            int p1Chips = getPlayerChipsValue(1);
            int p2Chips = getPlayerChipsValue(2);

            if (p1Chips == Int32.MaxValue)
            {
                return Int32.MinValue;
            }
            else if (p2Chips == Int32.MaxValue)
            {
                return Int32.MaxValue;
            }
            else
            {
                return getPlayerChipsValue(2) - getPlayerChipsValue(1);
            }
        }

        private int minMax(bool topLevel, int depth, bool maximizingPlayer)
        {
            if (_showAIThinking)
            {
                Task.Run(() => updateBoard()).Wait();
                Task.Delay(_aiSleepDelay).Wait();
                Debug.WriteLine("After run");
            }

            if (depth == 0 || isBoardFull())
            {
                return getBoardEvaluation();
            }
            else
            {
                /* This is laid out so that the boards aren't
                 * evaluated twice if the game is won.
                 * 
                 * (i.e. checking isGameWon() then returning getBoardEvaluation()
                 * is very inefficient; the game state is evaluated twice)
                 */

                int playerValue = getPlayerChipsValue(1);
                int aiValue = getPlayerChipsValue(2);

                if (Math.Abs(playerValue) == Int32.MaxValue || Math.Abs(aiValue) == Int32.MaxValue)
                {
                    if (playerValue == Int32.MaxValue)
                    {
                        return Int32.MinValue;
                    }
                    else
                    {
                        return Int32.MaxValue;
                    }
                }
            }

            if (maximizingPlayer)
            {
                int bestValue = Int32.MinValue;

                for (int i = 0; i < 7; ++i)
                {
                    if (!isColumnFull(i)) {
                        placeChip(2, i);
                        int value = minMax(false, depth - 1, false);
                        if (topLevel)
                        {

                            if (_showAIColVals)
                            {
                                Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                                {
                                    Debug.WriteLine("Col: " + i + " Value: " + value);
                                    TextBlock colValue = new TextBlock();
                                    colValue.HorizontalAlignment = HorizontalAlignment.Center;
                                    colValue.VerticalAlignment = VerticalAlignment.Center;
                                    colValue.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
                                    colValue.FontSize = 24;

                                    Color foregroundColor;

                                    if (value >= bestValue)
                                    {
                                        foregroundColor = Colors.Green;

                                        if (i != 0)
                                        {
                                            _prevBestVal.Foreground = new SolidColorBrush(Colors.Red);
                                        }

                                        _prevBestVal = colValue;
                                    }
                                    else
                                    {
                                        foregroundColor = Colors.Red;
                                    }

                                    colValue.Foreground = new SolidColorBrush(foregroundColor);

                                    string valText;
                                    if (value == Int32.MaxValue)
                                    {
                                        valText = "+Inf";
                                    }
                                    else if (value == Int32.MinValue)
                                    {
                                        valText = "-Inf";
                                    }
                                    else
                                    {
                                        valText = Convert.ToString(value);
                                    }

                                    colValue.Text = valText;
                                    colValue.SetValue(Grid.RowProperty, 0);
                                    colValue.SetValue(Grid.ColumnProperty, i);
                                    boardGrid.Children.Add(colValue);
                                }).AsTask().Wait();
                            }
                        }
                        if (value >= bestValue)
                        {
                            bestValue = value;
                            if (topLevel)
                            {
                                _bestMove = i;

                                _highestColVal = Math.Max(_highestColVal, bestValue);
                                _lowestColVal = Math.Min(_lowestColVal, bestValue);
                            }
                        }
                        removeChip(i);
                    }
                }

                return bestValue;
            }
            else
            {
                // Minimizing player
                int bestValue = Int32.MaxValue;
                
                for (int i = 0; i < 7; ++i)
                {
                    if (!isColumnFull(i))
                    {
                        placeChip(1, i);
                        int value = minMax(false, depth - 1, true);
                        bestValue = Math.Min(bestValue, value);
                        removeChip(i);
                    }
                }

                return bestValue;
            }
        }

        /* The offsets used when checking for potential 4-in-a-rows.
             * 
             * In this order:
             * Going...
             * - up, left
             * - up
             * - up, right
             * - right
             */
        int[,] _offsets = new int[,]
        {
                {-1, -1},
                {0, -1},
                {1, -1},
                {1, 0}
        };
        private int getPlayerChipsValue(int player)
        {
            /* Evaluation method:
             * 
             * -> 0 points for 0 in a row
             * -> 1 point for 1 in a row
             * -> 4 points for 2 in a row
             * -> 9 points for 3 in a row
             * -> infinte points for 4 in a row
             * 
             * Only chips with potential to be part of a four-in-a-row
             * will add value.
             * 
             * Chips with potential to be part of a four-in-a-row in multiple
             * directions will be counted multiple times.
             * 
             * Potential future improvement: less value given to potential four-in-a-rows
             * who need chips below them first (since they are harder to build).
             */

            int value = 0;
            
            int[] rewards = {0, 1, 8, 27, Int32.MaxValue};
            int otherPlayer = (player == 1 ? 2 : 1);

            for (int y = 0; y < 6; ++y)
            {
                for (int x = 0; x < 7; ++x)
                {
                    // Check each direction for a potential 4-in-a-row
                    for (int off = 0; off < _offsets.GetLength(0); ++off)
                    {
                        if (!inBounds(x + _offsets[off, 0] * 3, y + _offsets[off, 1] * 3))
                        {
                            continue;
                        }

                        /* The number of chips the player owns in this potential
                         * 4-in-a-row.
                         */
                        int playerChips = 0;
                        for (int offLen = 0; offLen < 4; ++offLen)
                        {
                            int currentChip = _board[y + _offsets[off, 1] * offLen, x + _offsets[off, 0] * offLen];

                            if (currentChip == otherPlayer)
                            {
                                /* The other player has a chip, so this player
                                 * cannot get a 4-in-a-row here.
                                 */
                                goto endOffsetLoop;
                            }

                            if (currentChip == player)
                            {
                                ++playerChips;
                            }
                        }
                        
                        if (playerChips == 4)
                        {
                            return rewards[4];
                        }
                        else
                        {
                            value += rewards[playerChips];
                        }

                        endOffsetLoop:;
                    }
                }
            }
            
            return value;
        }

        private bool inBounds(int x, int y)
        {
            return x >= 0 && x < 7
                && y >= 0 && y < 6;
        }

        public bool isGameWon()
        {
            return getPlayerChipsValue(2) == Int32.MaxValue
                || getPlayerChipsValue(1) == Int32.MaxValue;
        }

        private void SearchDepthSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            Slider depthSlider = (Slider)sender;
            _searchDepth = (int) depthSlider.Value;
        }

        private void StateDelaySlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            Slider delaySlider = (Slider)sender;
            _aiSleepDelay = (int)delaySlider.Value;
        }

        private void ShowThinkingCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            _showAIThinking = true;
        }

        private void ShowThinkingCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            _showAIThinking = false;
        }

        private void ShowColVals_Checked(object sender, RoutedEventArgs e)
        {
            _showAIColVals = true;
        }

        private async void ShowColVals_Unchecked(object sender, RoutedEventArgs e)
        {
            _showAIColVals = false;

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                for (int i = 0; i < boardGrid.Children.Count; ++i)
                {
                    if (boardGrid.Children.ElementAt(i) is TextBlock)
                    {
                        boardGrid.Children.RemoveAt(i);
                        i = 0;
                    }
                }
            });
        }

        private void RestartButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // clear board
            for (int y = 0; y < 6; ++y)
            {
                for (int x = 0; x < 7; ++x)
                {
                    _board[y, x] = 0;
                }
            }

            Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                for (int i = 0; i < boardGrid.Children.Count; ++i)
                {
                    if (boardGrid.Children.ElementAt(i) is TextBlock)
                    {
                        boardGrid.Children.RemoveAt(i);
                        i = 0;
                    }
                }

                StatusText2.Foreground = new SolidColorBrush(Colors.Red);
                StatusText2.Text = "Your turn";
            });

            updateBoard();
        }
    }
}
