using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ACT4
{
    public partial class Form1 : Form
    {
        int cellDimension;
        int queenCount = 6;

        SixState initialConfiguration;
        SixState[] beamConfigurations;

        int stepCount;
        int beamWidth;
        int optimalStateIndex;

        int[,,] heuristicValues;
        ArrayList[] bestPossibleMoves;
        Object[] selectedMoves;

        public Form1()
        {
            InitializeComponent();
            cellDimension = pictureBox1.Width / queenCount;
            initialConfiguration = GenerateRandomState();
            beamWidth = 3;
            optimalStateIndex = 0;

            beamConfigurations = new SixState[beamWidth];
            selectedMoves = new object[beamWidth];

            beamConfigurations[0] = new SixState(initialConfiguration);
            beamConfigurations[1] = GenerateRandomState();
            beamConfigurations[2] = GenerateRandomState();

            RefreshInterface();
            label1.Text = "Attacking pairs: " + CalculateConflicts(initialConfiguration);
        }

        /// <summary>
        /// Refreshes the UI elements based on the current beam configurations.
        /// </summary>
        private void RefreshInterface()
        {
            pictureBox2.Refresh();
            label3.Text = "Attacking pairs: " + CalculateConflicts(beamConfigurations[optimalStateIndex]);
            label4.Text = "Moves: " + stepCount;
            heuristicValues = CreateHeuristicMatrix(beamConfigurations);
            bestPossibleMoves = DetermineBestMoves(heuristicValues);

            listBox1.Items.Clear();

            for (int i = 0; i < beamWidth; i++)
                if (bestPossibleMoves[i].Count > 0)
                    selectedMoves[i] = SelectRandomMove(bestPossibleMoves[i]);

            foreach (Point move in bestPossibleMoves[optimalStateIndex])
            {
                listBox1.Items.Add(move);
            }

            label2.Text = "Selected move: " + selectedMoves[optimalStateIndex];
        }

        /// <summary>
        /// Paint event for pictureBox1 to visualize all beam states.
        /// </summary>
        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            // Define distinct colors for different beam configurations
            Brush[] queenBrushes = { Brushes.Fuchsia, Brushes.Green, Brushes.Red, Brushes.Yellow, Brushes.Orange, Brushes.Purple };

            for (int configIndex = 0; configIndex < beamWidth; configIndex++)
            {
                SixState config = beamConfigurations[configIndex];
                Brush currentBrush = queenBrushes[configIndex % queenBrushes.Length];

                for (int col = 0; col < queenCount; col++)
                {
                    int row = config.Y[col];
                    e.Graphics.FillEllipse(currentBrush, col * cellDimension, row * cellDimension, cellDimension, cellDimension);
                }
            }

            // Draw grid lines
            Pen gridPen = Pens.Black;
            for (int i = 0; i <= queenCount; i++)
            {
                // Vertical lines
                e.Graphics.DrawLine(gridPen, i * cellDimension, 0, i * cellDimension, queenCount * cellDimension);
                // Horizontal lines
                e.Graphics.DrawLine(gridPen, 0, i * cellDimension, queenCount * cellDimension, i * cellDimension);
            }
        }

        /// <summary>
        /// Paint event for pictureBox2 to visualize the best beam state.
        /// </summary>
        private void pictureBox2_Paint(object sender, PaintEventArgs e)
        {
            // Find the configuration with the least attacking pairs
            SixState bestConfig = beamConfigurations.OrderBy(s => CalculateConflicts(s)).FirstOrDefault();

            if (bestConfig == null)
                return;

            // Draw chessboard squares
            for (int col = 0; col < queenCount; col++)
            {
                for (int row = 0; row < queenCount; row++)
                {
                    if ((col + row) % 2 == 0)
                    {
                        e.Graphics.FillRectangle(Brushes.LightGray, col * cellDimension, row * cellDimension, cellDimension, cellDimension);
                    }
                    else
                    {
                        e.Graphics.FillRectangle(Brushes.White, col * cellDimension, row * cellDimension, cellDimension, cellDimension);
                    }
                }
            }

            // Draw queens for the best configuration
            Brush queenBrush = Brushes.Fuchsia;
            for (int col = 0; col < queenCount; col++)
            {
                int row = bestConfig.Y[col];
                e.Graphics.FillEllipse(queenBrush, col * cellDimension, row * cellDimension, cellDimension, cellDimension);
            }

            // Draw grid lines
            Pen gridPen = Pens.Black;
            for (int i = 0; i <= queenCount; i++)
            {
                // Vertical lines
                e.Graphics.DrawLine(gridPen, i * cellDimension, 0, i * cellDimension, queenCount * cellDimension);
                // Horizontal lines
                e.Graphics.DrawLine(gridPen, 0, i * cellDimension, queenCount * cellDimension, i * cellDimension);
            }
        }

        /// <summary>
        /// Generates a random SixState configuration.
        /// </summary>
        private SixState GenerateRandomState()
        {
            Random rand = new Random(Guid.NewGuid().GetHashCode());
            SixState randomConfig = new SixState(
                rand.Next(queenCount),
                rand.Next(queenCount),
                rand.Next(queenCount),
                rand.Next(queenCount),
                rand.Next(queenCount),
                rand.Next(queenCount)
            );
            return randomConfig;
        }

        /// <summary>
        /// Calculates the number of attacking queen pairs in a given configuration.
        /// </summary>
        private int CalculateConflicts(SixState config)
        {
            int conflicts = 0;

            for (int col = 0; col < queenCount; col++)
            {
                for (int targetCol = col + 1; targetCol < queenCount; targetCol++)
                {
                    // Horizontal conflicts
                    if (config.Y[col] == config.Y[targetCol])
                        conflicts++;

                    // Diagonal down conflicts
                    if (config.Y[targetCol] == config.Y[col] + targetCol - col)
                        conflicts++;

                    // Diagonal up conflicts
                    if (config.Y[col] == config.Y[targetCol] + targetCol - col)
                        conflicts++;
                }
            }

            return conflicts;
        }

        /// <summary>
        /// Creates a heuristic matrix for all beam configurations and their possible moves.
        /// </summary>
        private int[,,] CreateHeuristicMatrix(SixState[] currentConfigs)
        {
            int[,,] heuristics = new int[beamWidth, queenCount, queenCount];

            for (int configIdx = 0; configIdx < beamWidth; configIdx++)
            {
                for (int col = 0; col < queenCount; col++)
                {
                    for (int row = 0; row < queenCount; row++)
                    {
                        if (currentConfigs[configIdx].Y[col] == row)
                        {
                            heuristics[configIdx, col, row] = CalculateConflicts(currentConfigs[configIdx]);
                            continue; // Skip if the queen is already in this position
                        }

                        SixState possibleConfig = new SixState(currentConfigs[configIdx]);
                        possibleConfig.Y[col] = row;
                        heuristics[configIdx, col, row] = CalculateConflicts(possibleConfig);
                    }
                }
            }

            return heuristics;
        }

        /// <summary>
        /// Determines the best possible moves for each beam configuration based on the heuristic matrix.
        /// </summary>
        private ArrayList[] DetermineBestMoves(int[,,] heuristicMatrix)
        {
            ArrayList[] bestMovesArray = new ArrayList[beamWidth];
            for (int i = 0; i < beamWidth; i++)
                bestMovesArray[i] = new ArrayList();

            int[] bestHeuristicScores = new int[beamWidth];

            for (int configIdx = 0; configIdx < beamWidth; configIdx++)
            {
                bestHeuristicScores[configIdx] = heuristicMatrix[configIdx, 0, 0];
                for (int col = 0; col < queenCount; col++)
                {
                    for (int row = 0; row < queenCount; row++)
                    {
                        if (heuristicMatrix[configIdx, col, row] < bestHeuristicScores[configIdx])
                        {
                            bestHeuristicScores[configIdx] = heuristicMatrix[configIdx, col, row];
                            bestMovesArray[configIdx].Clear();
                            if (beamConfigurations[configIdx].Y[col] != row)
                                bestMovesArray[configIdx].Add(new Point(col, row));
                        }
                        else if (heuristicMatrix[configIdx, col, row] == bestHeuristicScores[configIdx])
                        {
                            if (beamConfigurations[configIdx].Y[col] != row)
                                bestMovesArray[configIdx].Add(new Point(col, row));
                        }
                    }
                }
            }

            // Identify the beam configuration with the lowest heuristic score
            for (int i = 0; i < beamWidth; i++)
                if (bestHeuristicScores[optimalStateIndex] > bestHeuristicScores[i])
                    optimalStateIndex = i;

            label5.Text = "Possible Moves (H=" + bestHeuristicScores[optimalStateIndex] + ")";
            return bestMovesArray;
        }

        /// <summary>
        /// Selects a random move from the list of possible best moves.
        /// </summary>
        private Object SelectRandomMove(ArrayList possibleMoves)
        {
            int moveCount = possibleMoves.Count;
            if (moveCount == 0)
                return null;

            Random rand = new Random(Guid.NewGuid().GetHashCode());
            int randomIndex = rand.Next(moveCount);

            return possibleMoves[randomIndex];
        }

        /// <summary>
        /// Applies a selected move to the optimal beam configuration.
        /// </summary>
        private void ApplyMoveToState(Point move)
        {
            for (int col = 0; col < queenCount; col++)
            {
                initialConfiguration.Y[col] = beamConfigurations[optimalStateIndex].Y[col];
            }
            beamConfigurations[optimalStateIndex].Y[move.X] = move.Y;
            stepCount++;

            for (int i = 0; i < beamWidth; i++)
                selectedMoves[i] = null;

            RefreshInterface();
        }

        /// <summary>
        /// Event handler for when the selected index of listBox1 changes.
        /// Updates the selected move label.
        /// </summary>
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                Point selectedMove = (Point)listBox1.SelectedItem;
                label2.Text = "Selected move: " + selectedMove.ToString();
            }
        }

        /// <summary>
        /// Event handler for the "Single Step" button click.
        /// Executes a single move on the optimal beam configuration.
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            if (CalculateConflicts(beamConfigurations[optimalStateIndex]) > 0)
                ApplyMoveToState((Point)selectedMoves[optimalStateIndex]);
        }

        /// <summary>
        /// Event handler for the "Reset" button click.
        /// Resets the beam configurations to new random states.
        /// </summary>
        private void button3_Click(object sender, EventArgs e)
        {
            beamConfigurations[0] = initialConfiguration = GenerateRandomState();
            for (int i = 1; i < beamWidth; i++)
                beamConfigurations[i] = new SixState();

            stepCount = 0;

            RefreshInterface();
            pictureBox1.Refresh();
            label1.Text = "Attacking pairs: " + CalculateConflicts(beamConfigurations[optimalStateIndex]);
        }

        /// <summary>
        /// Event handler for the "Run to Completion" button click.
        /// Continuously executes moves until a solution is found.
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            while (CalculateConflicts(beamConfigurations[optimalStateIndex]) > 0)
            {
                for (int i = 0; i < beamWidth; i++)
                    ApplyMoveToState((Point)selectedMoves[i]);
            }
        }

        /// <summary>
        /// Event handler for the form load event.
        /// Currently unused but required to eliminate designer errors.
        /// </summary>
        private void Form1_Load(object sender, EventArgs e)
        {
            // No additional initialization required at load time.
        }
    }
}
