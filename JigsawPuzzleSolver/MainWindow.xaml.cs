﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;
using System.IO;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;

namespace JigsawPuzzleSolver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            list_imageDescriptions.ItemsSource = ProcessedImagesStorage.ImageDescriptions;
        }

        PieceCollection_old pieces;
        Puzzle puzzle;

        private void btn_process_old_Click(object sender, RoutedEventArgs e)
        {
            pieces = PieceExtractor_old.ExtractPieces(@"..\..\..\Test_Pictures\Pieces1_and_2.jpg");
            //pieces = PieceExtractor.ExtractPieces(@"..\..\..\Test_Pictures\Pieces7.jpg");
            //pieces = PieceExtractor.ExtractPieces(@"..\..\..\Test_Pictures\Piece2_1.jpg");
            //pieces = PieceExtractor.ExtractPieces(@"..\..\..\Test_Pictures\Piece3_3_1.jpg");
            //pieces = PieceExtractor.ExtractPieces(@"..\..\..\Test_Pictures\Piece3_2_1.jpg");
            //pieces = PieceExtractor.ExtractPieces(@"..\..\..\Test_Pictures\Pieces10.jpg");
            //pieces = PieceExtractor.ExtractPieces(@"..\..\..\Test_Pictures\ExtractedPieces\Pieces10__Piece#2.jpg");
            
            //pieces.SavePieces(@"..\..\..\Test_Pictures\ExtractedPieces");

            pieces.InitPieceEdgesForAllPieces();

            pieces[0].Edges[0].CalculateEdgeMatchFactor(pieces[0].Edges[0]);
            pieces[0].Edges[2].CalculateEdgeMatchFactor(pieces[3].Edges[0]);

            /*foreach (Piece piece1 in pieces)
            {
                foreach (PieceEdge pieceEdge1 in piece1.Edges)
                {
                    foreach (Piece piece2 in pieces)
                    {
                        foreach (PieceEdge pieceEdge2 in piece2.Edges)
                        {
                            pieceEdge1.CalculateEdgeMatchFactor(pieceEdge2);
                        }
                    }
                }
            }*/

        }
        
        private void btn_init_puzzle_Click(object sender, RoutedEventArgs e)
        {
            //puzzle = new Puzzle(@"..\..\..\Test_Pictures\ScannedImages", 20, 50, false);

            //puzzle = new Puzzle(@"..\..\..\Scans\AngryBirds\ScannerOpen\1.tiff", 20, 50, true);
            puzzle = new Puzzle(@"..\..\..\Scans\ToyStoryBack\4.tiff", 20, 50, true);
        }

        private void btn_solve_puzzle_Click(object sender, RoutedEventArgs e)
        {
            puzzle.solve();
        }

        private void list_imageDescriptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            img_Processed.Source = Utils.BitmapToImageSource(ProcessedImagesStorage.GetImage(list_imageDescriptions.SelectedItem.ToString()));
        }
    }
}
