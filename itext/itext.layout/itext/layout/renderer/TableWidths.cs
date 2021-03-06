/*
This file is part of the iText (R) project.
Copyright (c) 1998-2017 iText Group NV
Authors: iText Software.

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License version 3
as published by the Free Software Foundation with the addition of the
following permission added to Section 15 as permitted in Section 7(a):
FOR ANY PART OF THE COVERED WORK IN WHICH THE COPYRIGHT IS OWNED BY
ITEXT GROUP. ITEXT GROUP DISCLAIMS THE WARRANTY OF NON INFRINGEMENT
OF THIRD PARTY RIGHTS

This program is distributed in the hope that it will be useful, but
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
or FITNESS FOR A PARTICULAR PURPOSE.
See the GNU Affero General Public License for more details.
You should have received a copy of the GNU Affero General Public License
along with this program; if not, see http://www.gnu.org/licenses or write to
the Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
Boston, MA, 02110-1301 USA, or download the license from the following URL:
http://itextpdf.com/terms-of-use/

The interactive user interfaces in modified source and object code versions
of this program must display Appropriate Legal Notices, as required under
Section 5 of the GNU Affero General Public License.

In accordance with Section 7(b) of the GNU Affero General Public License,
a covered work must retain the producer line in every PDF that is created
or manipulated using iText.

You can be released from the requirements of the license by purchasing
a commercial license. Buying such a license is mandatory as soon as you
develop commercial activities involving the iText software without
disclosing the source code of your own applications.
These activities include: offering paid services to customers as an ASP,
serving PDFs on the fly in a web application, shipping iText with a closed
source product.

For more information, please contact iText Software Corp. at this
address: sales@itextpdf.com
*/
using System;
using System.Collections.Generic;
using iText.IO.Log;
using iText.IO.Util;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Minmaxwidth;
using iText.Layout.Properties;

namespace iText.Layout.Renderer {
    internal sealed class TableWidths {
        private TableRenderer tableRenderer;

        private int numberOfColumns;

        private float rightBorderMaxWidth;

        private float leftBorderMaxWidth;

        private TableWidths.ColumnWidthData[] widths;

        private IList<TableWidths.CellInfo> cells;

        private float tableWidth;

        private bool fixedTableWidth;

        private bool fixedTableLayout = false;

        private float minWidth;

        internal TableWidths(TableRenderer tableRenderer, float availableWidth, bool calculateTableMaxWidth, float
             rightBorderMaxWidth, float leftBorderMaxWidth) {
            this.tableRenderer = tableRenderer;
            numberOfColumns = ((Table)tableRenderer.GetModelElement()).GetNumberOfColumns();
            this.rightBorderMaxWidth = rightBorderMaxWidth;
            this.leftBorderMaxWidth = leftBorderMaxWidth;
            CalculateTableWidth(availableWidth, calculateTableMaxWidth);
        }

        internal bool HasFixedLayout() {
            return fixedTableLayout;
        }

        internal float GetMinWidth() {
            return minWidth;
        }

        internal float[] AutoLayout(float[] minWidths, float[] maxWidths) {
            FillWidths(minWidths, maxWidths);
            FillAndSortCells();
            float minSum = 0;
            foreach (TableWidths.ColumnWidthData width in widths) {
                minSum += width.min;
            }
            //region Process cells
            bool[] minColumns = new bool[numberOfColumns];
            foreach (TableWidths.CellInfo cell in cells) {
                //NOTE in automatic layout algorithm percents have higher priority
                UnitValue cellWidth = cell.GetWidth();
                if (cellWidth != null && cellWidth.GetValue() >= 0) {
                    if (cellWidth.IsPercentValue()) {
                        //cellWidth has percent value
                        if (cell.GetColspan() == 1) {
                            widths[cell.GetCol()].SetPercents(cellWidth.GetValue());
                        }
                        else {
                            int pointColumns = 0;
                            float percentSum = 0;
                            for (int i = cell.GetCol(); i < cell.GetCol() + cell.GetColspan(); i++) {
                                if (!widths[i].isPercent) {
                                    pointColumns++;
                                }
                                else {
                                    percentSum += widths[i].width;
                                }
                            }
                            float percentAddition = cellWidth.GetValue() - percentSum;
                            if (percentAddition > 0) {
                                if (pointColumns == 0) {
                                    //ok, add percents to each column
                                    for (int i = cell.GetCol(); i < cell.GetCol() + cell.GetColspan(); i++) {
                                        widths[i].AddPercents(percentAddition / cell.GetColspan());
                                    }
                                }
                                else {
                                    // set percent only to cells without one
                                    for (int i = cell.GetCol(); i < cell.GetCol() + cell.GetColspan(); i++) {
                                        if (!widths[i].isPercent) {
                                            widths[i].SetPercents(percentAddition / pointColumns).SetFixed(true);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else {
                        //cellWidth has point value
                        if (cell.GetCol() == 1) {
                            if (!widths[cell.GetCol()].isPercent) {
                                widths[cell.GetCol()].SetPoints(cellWidth.GetValue()).SetFixed(true);
                                if (widths[cell.GetCol()].HasCollision()) {
                                    minColumns[cell.GetCol()] = true;
                                }
                            }
                        }
                        else {
                            int flexibleCols = 0;
                            float colspanRemain = cellWidth.GetValue();
                            for (int i = cell.GetCol(); i < cell.GetCol() + cell.GetColspan(); i++) {
                                if (!widths[i].isPercent) {
                                    colspanRemain -= widths[i].width;
                                    if (!widths[i].isFixed) {
                                        flexibleCols++;
                                    }
                                }
                                else {
                                    colspanRemain = -1;
                                    break;
                                }
                            }
                            if (colspanRemain > 0) {
                                if (flexibleCols > 0) {
                                    // check min width in columns
                                    for (int i = cell.GetCol(); i < cell.GetCol() + cell.GetColspan(); i++) {
                                        if (!widths[i].isFixed && widths[i].CheckCollision(colspanRemain / flexibleCols)) {
                                            widths[i].SetPoints(widths[i].min).SetFixed(true);
                                            if ((colspanRemain -= widths[i].min) <= 0 || flexibleCols-- <= 0) {
                                                break;
                                            }
                                        }
                                    }
                                    if (colspanRemain > 0 && flexibleCols > 0) {
                                        for (int k = cell.GetCol(); k < cell.GetCol() + cell.GetColspan(); k++) {
                                            if (!widths[k].isFixed) {
                                                widths[k].AddPoints(colspanRemain / flexibleCols).SetFixed(true);
                                            }
                                        }
                                    }
                                }
                                else {
                                    for (int i = cell.GetCol(); i < cell.GetCol() + cell.GetColspan(); i++) {
                                        widths[i].AddPoints(colspanRemain / cell.GetColspan());
                                    }
                                }
                            }
                        }
                    }
                }
                else {
                    if (!widths[cell.GetCol()].isFixed) {
                        int flexibleCols = 0;
                        float remainWidth = 0;
                        //if there is no information, try to set max width
                        for (int i = cell.GetCol(); i < cell.GetCol() + cell.GetColspan(); i++) {
                            if (!widths[i].isFixed && !widths[i].isPercent) {
                                remainWidth += widths[i].max - widths[i].width;
                                flexibleCols++;
                            }
                        }
                        if (remainWidth > 0) {
                            if (flexibleCols > 0) {
                                for (int i = cell.GetCol(); i < cell.GetCol() + cell.GetColspan(); i++) {
                                    if (!widths[i].isFixed && !widths[i].isPercent) {
                                        widths[i].AddPoints(remainWidth / flexibleCols);
                                    }
                                }
                            }
                            else {
                                for (int k = cell.GetCol(); k < cell.GetCol() + cell.GetColspan(); k++) {
                                    widths[k].AddPoints(remainWidth / cell.GetColspan());
                                }
                            }
                        }
                    }
                }
            }
            for (int col = 0; col < minColumns.Length; col++) {
                if (minColumns[col] && !widths[col].isPercent && widths[col].isFixed && widths[col].HasCollision()) {
                    minSum += widths[col].min - widths[col].width;
                    widths[col].SetPoints(widths[col].min);
                }
            }
            //endregion
            //region Process columns
            //TODO add colgroup information.
            for (int i = 0; i < numberOfColumns; i++) {
                UnitValue colWidth = GetTable().GetColumnWidth(i);
                if (colWidth.GetValue() >= 0) {
                    if (colWidth.IsPercentValue()) {
                        if (!widths[i].isPercent && widths[i].isFixed && widths[i].width > widths[i].min) {
                            widths[i].max = widths[i].width;
                            widths[i].SetFixed(false);
                        }
                        if (!widths[i].isPercent) {
                            widths[i].SetPercents(colWidth.GetValue());
                        }
                    }
                    else {
                        if (!widths[i].isPercent && colWidth.GetValue() >= widths[i].min) {
                            if (widths[i].isFixed) {
                                widths[i].SetPoints(colWidth.GetValue());
                            }
                            else {
                                widths[i].ResetPoints(colWidth.GetValue());
                            }
                        }
                    }
                }
            }
            //endregion
            // region recalculate
            if (tableWidth - minSum < 0) {
                for (int i = 0; i < numberOfColumns; i++) {
                    widths[i].finalWidth = widths[i].min;
                }
            }
            else {
                float sumOfPercents = 0;
                // minTableWidth include only non percent columns.
                float minTableWidth = 0;
                float totalNonPercent = 0;
                // validate sumOfPercents, last columns will be set min width, if sum > 100.
                for (int i = 0; i < widths.Length; i++) {
                    if (widths[i].isPercent) {
                        if (sumOfPercents < 100 && sumOfPercents + widths[i].width > 100) {
                            widths[i].width -= sumOfPercents + widths[i].width - 100;
                            sumOfPercents += widths[i].width;
                            Warn100percent();
                        }
                        else {
                            if (sumOfPercents >= 100) {
                                widths[i].ResetPoints(widths[i].min);
                                minTableWidth += widths[i].width;
                                Warn100percent();
                            }
                            else {
                                sumOfPercents += widths[i].width;
                            }
                        }
                    }
                    else {
                        minTableWidth += widths[i].min;
                        totalNonPercent += widths[i].width;
                    }
                }
                System.Diagnostics.Debug.Assert(sumOfPercents <= 100);
                bool toBalance = true;
                if (!fixedTableWidth) {
                    float tableWidthBasedOnPercents = sumOfPercents < 100 ? totalNonPercent * 100 / (100 - sumOfPercents) : 0;
                    for (int i = 0; i < numberOfColumns; i++) {
                        if (widths[i].isPercent) {
                            tableWidthBasedOnPercents = Math.Max(widths[i].max * 100 / widths[i].width, tableWidthBasedOnPercents);
                        }
                    }
                    if (tableWidthBasedOnPercents <= tableWidth) {
                        tableWidth = tableWidthBasedOnPercents;
                        //we don't need more space, columns are done.
                        toBalance = false;
                    }
                }
                if (sumOfPercents < 100 && totalNonPercent == 0) {
                    // each column has percent value but sum < 100%
                    // upscale percents
                    for (int i = 0; i < widths.Length; i++) {
                        widths[i].width = 100 * widths[i].width / sumOfPercents;
                    }
                    sumOfPercents = 100;
                }
                if (!toBalance) {
                    for (int i = 0; i < numberOfColumns; i++) {
                        widths[i].finalWidth = widths[i].isPercent ? tableWidth * widths[i].width / 100 : widths[i].width;
                    }
                }
                else {
                    if (sumOfPercents >= 100) {
                        sumOfPercents = 100;
                        bool recalculatePercents = false;
                        float remainingWidth = tableWidth - minTableWidth;
                        for (int i = 0; i < numberOfColumns; i++) {
                            if (widths[i].isPercent) {
                                if (remainingWidth * widths[i].width >= widths[i].min) {
                                    widths[i].finalWidth = remainingWidth * widths[i].width / 100;
                                }
                                else {
                                    widths[i].finalWidth = widths[i].min;
                                    widths[i].isPercent = false;
                                    remainingWidth -= widths[i].min;
                                    sumOfPercents -= widths[i].width;
                                    recalculatePercents = true;
                                }
                            }
                            else {
                                widths[i].finalWidth = widths[i].min;
                            }
                        }
                        if (recalculatePercents) {
                            for (int i = 0; i < numberOfColumns; i++) {
                                if (widths[i].isPercent) {
                                    widths[i].finalWidth = remainingWidth * widths[i].width / sumOfPercents;
                                }
                            }
                        }
                    }
                    else {
                        // We either have some extra space and may extend columns in case fixed table width,
                        // or have to decrease columns to fit table width.
                        //
                        // columns shouldn't be more than its max value in case unspecified table width.
                        //columns shouldn't be more than its percentage value.
                        // opposite to sumOfPercents, which is sum of percent values.
                        float totalPercent = 0;
                        float minTotalNonPercent = 0;
                        float fixedAddition = 0;
                        float flexibleAddition = 0;
                        //sum of non fixed non percent columns.
                        for (int i = 0; i < numberOfColumns; i++) {
                            if (widths[i].isPercent) {
                                if (tableWidth * widths[i].width >= widths[i].min) {
                                    widths[i].finalWidth = tableWidth * widths[i].width / 100;
                                    totalPercent += widths[i].finalWidth;
                                }
                                else {
                                    sumOfPercents -= widths[i].width;
                                    widths[i].ResetPoints(widths[i].min);
                                    widths[i].finalWidth = widths[i].min;
                                    minTotalNonPercent += widths[i].min;
                                }
                            }
                            else {
                                widths[i].finalWidth = widths[i].min;
                                minTotalNonPercent += widths[i].min;
                                float addition = widths[i].width - widths[i].min;
                                if (widths[i].isFixed) {
                                    fixedAddition += addition;
                                }
                                else {
                                    flexibleAddition += addition;
                                }
                            }
                        }
                        if (totalPercent + minTotalNonPercent > tableWidth) {
                            // collision between minWidth and percent value.
                            float extraWidth = tableWidth - minTotalNonPercent;
                            if (sumOfPercents > 0) {
                                for (int i = 0; i < numberOfColumns; i++) {
                                    if (widths[i].isPercent) {
                                        widths[i].finalWidth = extraWidth * widths[i].width / sumOfPercents;
                                    }
                                }
                            }
                        }
                        else {
                            float extraWidth = tableWidth - totalPercent - minTotalNonPercent;
                            if (fixedAddition > 0 && (extraWidth < fixedAddition || flexibleAddition == 0)) {
                                for (int i = 0; i < numberOfColumns; i++) {
                                    if (!widths[i].isPercent && widths[i].isFixed) {
                                        widths[i].finalWidth += (widths[i].width - widths[i].min) * extraWidth / fixedAddition;
                                    }
                                }
                            }
                            else {
                                extraWidth -= fixedAddition;
                                if (extraWidth < flexibleAddition) {
                                    for (int i = 0; i < numberOfColumns; i++) {
                                        if (!widths[i].isPercent) {
                                            if (widths[i].isFixed) {
                                                widths[i].finalWidth = widths[i].width;
                                            }
                                            else {
                                                widths[i].finalWidth += (widths[i].width - widths[i].min) * extraWidth / flexibleAddition;
                                            }
                                        }
                                    }
                                }
                                else {
                                    float totalFixed = 0;
                                    float totalFlexible = 0;
                                    for (int i = 0; i < numberOfColumns; i++) {
                                        if (!widths[i].isPercent) {
                                            if (widths[i].isFixed) {
                                                widths[i].finalWidth = widths[i].width;
                                                totalFixed += widths[i].width;
                                            }
                                            else {
                                                totalFlexible += widths[i].width;
                                            }
                                        }
                                    }
                                    extraWidth = tableWidth - totalPercent - totalFixed;
                                    for (int i = 0; i < numberOfColumns; i++) {
                                        if (!widths[i].isPercent && !widths[i].isFixed) {
                                            widths[i].finalWidth = widths[i].width * extraWidth / totalFlexible;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            //endregion
            return ExtractWidths();
        }

        internal float[] FixedLayout() {
            float[] columnWidths = new float[numberOfColumns];
            //fill columns from col info
            for (int i = 0; i < numberOfColumns; i++) {
                UnitValue colWidth = GetTable().GetColumnWidth(i);
                if (colWidth == null || colWidth.GetValue() < 0) {
                    columnWidths[i] = -1;
                }
                else {
                    if (colWidth.IsPercentValue()) {
                        columnWidths[i] = colWidth.GetValue() * tableWidth / 100;
                    }
                    else {
                        columnWidths[i] = colWidth.GetValue();
                    }
                }
            }
            //fill columns with -1 from cell info.
            int processedColumns = 0;
            float remainWidth = tableWidth;
            for (int i = 0; i < numberOfColumns; i++) {
                if (columnWidths[i] == -1) {
                    CellRenderer cell = tableRenderer.rows[0][i];
                    if (cell != null) {
                        float? cellWidth = cell.RetrieveUnitValue(tableWidth, Property.WIDTH);
                        if (cellWidth != null && cellWidth >= 0) {
                            int colspan = ((Cell)cell.GetModelElement()).GetColspan();
                            for (int j = 0; j < colspan; j++) {
                                columnWidths[i + j] = (float)cellWidth / colspan;
                            }
                            remainWidth -= columnWidths[i];
                            processedColumns++;
                        }
                    }
                }
                else {
                    remainWidth -= columnWidths[i];
                    processedColumns++;
                }
            }
            if (remainWidth > 0) {
                if (numberOfColumns == processedColumns) {
                    //Set remainWidth to all columns.
                    for (int i = 0; i < numberOfColumns; i++) {
                        columnWidths[i] += remainWidth / numberOfColumns;
                    }
                }
                else {
                    // Set all remain width to the unprocessed columns.
                    for (int i = 0; i < numberOfColumns; i++) {
                        if (columnWidths[i] == -1) {
                            columnWidths[i] = remainWidth / (numberOfColumns - processedColumns);
                        }
                    }
                }
            }
            else {
                if (numberOfColumns != processedColumns) {
                    //TODO shall we add warning?
                    for (int i = 0; i < numberOfColumns; i++) {
                        if (columnWidths[i] == -1) {
                            columnWidths[i] = 0;
                        }
                    }
                }
            }
            return columnWidths;
        }

        //region Common methods
        private void CalculateTableWidth(float availableWidth, bool calculateTableMaxWidth) {
            fixedTableLayout = "fixed".Equals(tableRenderer.GetProperty<String>(Property.TABLE_LAYOUT, "auto").ToLowerInvariant
                ());
            UnitValue width = tableRenderer.GetProperty<UnitValue>(Property.WIDTH);
            if (fixedTableLayout && width != null && width.GetValue() >= 0) {
                fixedTableWidth = true;
                tableWidth = RetrieveTableWidth(width, availableWidth);
                minWidth = width.IsPercentValue() ? 0 : tableWidth;
            }
            else {
                fixedTableLayout = false;
                //min width will initialize later
                minWidth = -1;
                if (calculateTableMaxWidth) {
                    fixedTableWidth = false;
                    tableWidth = RetrieveTableWidth(availableWidth);
                }
                else {
                    if (width != null && width.GetValue() >= 0) {
                        fixedTableWidth = true;
                        tableWidth = RetrieveTableWidth(width, availableWidth);
                    }
                    else {
                        fixedTableWidth = false;
                        tableWidth = RetrieveTableWidth(availableWidth);
                    }
                }
            }
        }

        private float RetrieveTableWidth(UnitValue width, float availableWidth) {
            return RetrieveTableWidth(width.IsPercentValue() ? width.GetValue() * availableWidth / 100 : width.GetValue
                ());
        }

        private float RetrieveTableWidth(float width) {
            float result = width - rightBorderMaxWidth / 2 - leftBorderMaxWidth / 2;
            return result > 0 ? result : 0;
        }

        private Table GetTable() {
            return (Table)tableRenderer.GetModelElement();
        }

        //endregion
        //region Auto layout utils
        private void FillWidths(float[] minWidths, float[] maxWidths) {
            widths = new TableWidths.ColumnWidthData[minWidths.Length];
            for (int i = 0; i < widths.Length; i++) {
                widths[i] = new TableWidths.ColumnWidthData(minWidths[i], maxWidths[i]);
            }
        }

        private void FillAndSortCells() {
            cells = new List<TableWidths.CellInfo>();
            if (tableRenderer.headerRenderer != null) {
                FillRendererCells(tableRenderer.headerRenderer, TableWidths.CellInfo.HEADER);
            }
            FillRendererCells(tableRenderer, TableWidths.CellInfo.BODY);
            if (tableRenderer.footerRenderer != null) {
                FillRendererCells(tableRenderer.footerRenderer, TableWidths.CellInfo.FOOTER);
            }
            // Cells are sorted, because we need to process cells without colspan
            // and process from top left to bottom right for other cases.
            JavaCollectionsUtil.Sort(cells);
        }

        private void FillRendererCells(TableRenderer renderer, byte region) {
            for (int row = 0; row < renderer.rows.Count; row++) {
                for (int col = 0; col < numberOfColumns; col++) {
                    CellRenderer cell = renderer.rows[row][col];
                    if (cell != null) {
                        cells.Add(new TableWidths.CellInfo(cell, region));
                    }
                }
            }
        }

        private void Warn100percent() {
            ILogger logger = LoggerFactory.GetLogger(typeof(iText.Layout.Renderer.TableWidths));
            logger.Warn(iText.IO.LogMessageConstant.SUM_OF_TABLE_COLUMNS_IS_GREATER_THAN_100);
        }

        private float[] ExtractWidths() {
            float actualWidth = 0;
            minWidth = 0;
            float[] columnWidths = new float[widths.Length];
            for (int i = 0; i < widths.Length; i++) {
                System.Diagnostics.Debug.Assert(widths[i].finalWidth >= 0);
                columnWidths[i] = widths[i].finalWidth;
                actualWidth += widths[i].finalWidth;
                minWidth += widths[i].min;
            }
            if (actualWidth > tableWidth + MinMaxWidthUtils.GetEps() * widths.Length) {
                ILogger logger = LoggerFactory.GetLogger(typeof(iText.Layout.Renderer.TableWidths));
                logger.Warn(iText.IO.LogMessageConstant.TABLE_WIDTH_IS_MORE_THAN_EXPECTED_DUE_TO_MIN_WIDTH);
            }
            return columnWidths;
        }

        //endregion
        //region Internal classes
        public override String ToString() {
            return "width=" + tableWidth + (fixedTableWidth ? "!!" : "");
        }

        private class ColumnWidthData {
            internal readonly float min;

            internal float max;

            internal float width = 0;

            internal float finalWidth = -1;

            internal bool isPercent = false;

            internal bool isFixed = false;

            internal ColumnWidthData(float min, float max) {
                //true means that this column has cell property based width.
                System.Diagnostics.Debug.Assert(min >= 0);
                System.Diagnostics.Debug.Assert(max >= 0);
                this.min = min > 0 ? min + MinMaxWidthUtils.GetEps() : 0;
                // All browsers implement a size limit on the cell's max width.
                // This limit is based on KHTML's representation that used 16 bits widths.
                this.max = max > 0 ? Math.Min(max + MinMaxWidthUtils.GetEps(), 32760) : 0;
            }

            internal virtual TableWidths.ColumnWidthData SetPoints(float width) {
                System.Diagnostics.Debug.Assert(!isPercent);
                this.width = Math.Max(this.width, width);
                return this;
            }

            internal virtual TableWidths.ColumnWidthData ResetPoints(float width) {
                this.width = width;
                this.isPercent = false;
                return this;
            }

            internal virtual TableWidths.ColumnWidthData AddPoints(float width) {
                System.Diagnostics.Debug.Assert(!isPercent);
                this.width += width;
                return this;
            }

            internal virtual TableWidths.ColumnWidthData SetPercents(float percent) {
                if (isPercent) {
                    width = Math.Max(width, percent);
                }
                else {
                    isPercent = true;
                    width = percent;
                }
                return this;
            }

            internal virtual TableWidths.ColumnWidthData AddPercents(float width) {
                System.Diagnostics.Debug.Assert(isPercent);
                this.width += width;
                return this;
            }

            internal virtual TableWidths.ColumnWidthData SetFixed(bool @fixed) {
                this.isFixed = @fixed;
                return this;
            }

            /// <summary>Check collusion between min value and point width</summary>
            /// <returns>
            /// true, if
            /// <see cref="min"/>
            /// greater than
            /// <see cref="width"/>
            /// .
            /// </returns>
            internal virtual bool HasCollision() {
                System.Diagnostics.Debug.Assert(!isPercent);
                return min > width;
            }

            /// <summary>Check collusion between min value and available point width.</summary>
            /// <param name="availableWidth">additional available point width.</param>
            /// <returns>
            /// true, if
            /// <see cref="min"/>
            /// greater than (
            /// <see cref="width"/>
            /// + additionalWidth).
            /// </returns>
            internal virtual bool CheckCollision(float availableWidth) {
                System.Diagnostics.Debug.Assert(!isPercent);
                return min > width + availableWidth;
            }

            public override String ToString() {
                return "w=" + width + (isPercent ? "%" : "pt") + (isFixed ? " !!" : "") + ", min=" + min + ", max=" + max 
                    + ", finalWidth=" + finalWidth;
            }
        }

        private class CellInfo : IComparable<TableWidths.CellInfo> {
            internal const byte HEADER = 1;

            internal const byte BODY = 2;

            internal const byte FOOTER = 3;

            private CellRenderer cell;

            private byte region;

            internal CellInfo(CellRenderer cell, byte region) {
                this.cell = cell;
                this.region = region;
            }

            internal virtual CellRenderer GetCell() {
                return cell;
            }

            internal virtual int GetCol() {
                return ((Cell)cell.GetModelElement()).GetCol();
            }

            internal virtual int GetColspan() {
                return ((Cell)cell.GetModelElement()).GetColspan();
            }

            internal virtual int GetRow() {
                return ((Cell)cell.GetModelElement()).GetRow();
            }

            internal virtual int GetRowspan() {
                return ((Cell)cell.GetModelElement()).GetRowspan();
            }

            //TODO DEVSIX-1057, DEVSIX-1021
            internal virtual UnitValue GetWidth() {
                UnitValue widthValue = cell.GetProperty<UnitValue>(Property.WIDTH);
                if (widthValue == null || widthValue.IsPercentValue()) {
                    return widthValue;
                }
                else {
                    Border[] borders = cell.GetBorders();
                    if (borders[1] != null) {
                        widthValue.SetValue(widthValue.GetValue() + borders[1].GetWidth() / 2);
                    }
                    if (borders[3] != null) {
                        widthValue.SetValue(widthValue.GetValue() + borders[3].GetWidth() / 2);
                    }
                    float[] paddings = cell.GetPaddings();
                    widthValue.SetValue(widthValue.GetValue() + paddings[1] + paddings[3]);
                    return widthValue;
                }
            }

            public virtual int CompareTo(TableWidths.CellInfo o) {
                if (GetColspan() == 1 ^ o.GetColspan() == 1) {
                    return GetColspan() - o.GetColspan();
                }
                if (region == o.region && GetRow() == o.GetRow()) {
                    return GetCol() + GetColspan() - o.GetCol() - o.GetColspan();
                }
                return region == o.region ? GetRow() - o.GetRow() : region - o.region;
            }
        }
        //endregion
    }
}
