import os
import sys
import pandas as pd
from plotnine import ggplot, aes, facet_grid, labs, geom_point, geom_smooth, guides, guide_legend, theme_linedraw, \
    geom_line, geom_boxplot

input_file = ""
output_file = ""
x_axis_column = ""
y_axis_column = ""
color_dimension = ""
facet_dimension = ""
graph_type = ""

def parse_arguments(args):
    flags = {} #dictionary to hold key/value pairs (flag/argument)
    current_flag = None

    for arg in args[1:]:  #everything after arg[1] which is the name of the script "graph.py"
        if arg == '-h' and len(args) > 2:
            print("Error! -h must be the only argument when it is included!")
            return
        elif arg.startswith('-'): #parse on flags dennoted by dashes
            current_flag = arg
            flags[current_flag] = [] #array that holds the flags specific (ex. -i, -o)
        elif current_flag:
            flags[current_flag].append(arg) #add argument to the dictionary with corresponding flag
        else:
            print(f"Invalid argument: {arg}")

    return flags

#add help menu info
#explain syntax, list dimension options, handled file extensions, etc.
#SPECIFY THAT DIMENSION NAMES MUST HAVE QUOTES AROUND THEM
def print_help():
    #check to make sure -h is the only command inlcuded
    #python3 graph.py -h
    print("\nHELP MENU:")
    print("-i : Input File Name. Input file types include .csv, .xlsx, or .json. Include the file name with its extension.")
    print("-d1: The x dimension for the graph. This is which column will be the x axis. Dimension names must have quotes around them ONLY IF there is whitespace in the column label!")
    print("-d2: The y dimension for the graph. This is which column will be the y axis. Dimension names must have quotes around them ONLY IF there is whitespace in the column label!")
    print("-d3: The dimension being used as the legend for the output graph. This flag is optional. Dimension names must have quotes around them ONLY IF there is whitespace in the column label!")
    print("-d4: The dimension facets for the graph. This flag is optional. Dimension names must have quotes around them ONLY IF there is whitespace in the column label!")
    print("-o: Output File Name. Output file types include .eps, .png, or .pdf. Include the file name with its extension.")
    print("-h: Prints a help menu to tell the user what each flag does and their inputs. When this argument is used, it has to be the only one.\n")
    exit(0)
    return


def set_variables(arguments):
    global input_file
    global x_axis_column
    global y_axis_column
    global color_dimension
    global facet_dimension
    global output_file
    global graph_type

    for flag, values in arguments.items():
        if flag == '-h':
            print_help()
        elif flag == '-i':
            input_file = arguments['-i'][0]
            df = pd.read_csv(input_file)  # Read the input file to get the column names
            df = df.rename(columns=lambda x: x.strip())  # Strip column labels of whitespace
        elif flag == '-d1':
            x_axis_column = find_matching_column(arguments['-d1'][0], df.columns)  # Convert and find matching column
        elif flag == '-d2':
            y_axis_column = find_matching_column(arguments['-d2'][0], df.columns)  # Convert and find matching column
        elif flag == '-d3':
            color_dimension = find_matching_column(arguments['-d3'][0], df.columns)  # Convert and find matching column
        elif flag == '-d4':
            facet_dimension = find_matching_column(arguments['-d4'][0], df.columns)  # Convert and find matching column
        elif flag == '-g':
            graph_type = arguments['-g'][0]
        elif flag == '-o':
            output_file = arguments['-o'][0]
        else:
            print("No valid action specified.")

    return

def find_matching_column(user_input, columns):
    user_input_lower = user_input.lower()

    # Check if the exact user input matches any column name
    if user_input_lower in map(str.lower, columns):
        return next(col for col in columns if col.lower() == user_input_lower)

    # If exact match not found, find the closest matching column name
    for column in columns:
        if user_input_lower == column.lower():
            return column

    return user_input


def main():
    global input_file
    global x_axis_column
    global y_axis_column
    global color_dimension
    global facet_dimension
    global output_file
    global graph_type

    arguments = parse_arguments(sys.argv)
    set_variables(arguments)


    #FOR TESTING
    print("Parsed arguments:")
    for flag, values in arguments.items():
        print(f"Flag: {flag}, Values: {values}")


    # Reading dataset
    df = pd.read_csv(input_file)
    # strip column labels of whitespace
    df = df.rename(columns=lambda x: x.strip())

    # Check if the specified columns exist in the dataset
    if x_axis_column not in df.columns:
        print(f"Error: Column '{x_axis_column}' does not exist in the dataset.")
        sys.exit(1)

    if y_axis_column not in df.columns:
        print(f"Error: Column '{y_axis_column}' does not exist in the dataset.")
        sys.exit(1)

    if '-d1' not in arguments and '-d2' not in arguments:
        print("ERROR: you must select at least an X and Y axis")
        return

    #build the plot
    #will later become separate functions to handle different numbers of arguments
    if arguments['-g'][0].lower() == "scatter":
        plot = (
                ggplot(df, aes(x=x_axis_column, y=y_axis_column, color=color_dimension))
                + geom_point(alpha=1)  # Set alpha to 1 to make points fully opaque
                + geom_smooth(method='lm', alpha=0.1)  # Set alpha to 1 to make the line fully opaque
                + labs(x=x_axis_column, y=y_axis_column)
                + facet_grid(facet_dimension)  #TODO: Temporarily removed color=color_dimension due to error # causing weird box
                + guides(color=guide_legend(nrow=5))  #TODO: this line causes errors when setting to scatter # Adjust the number of rows in the legend
                + theme_linedraw()
        )
    elif arguments['-g'][0].lower() == "box":
        plot = (
            ggplot(df, aes(x=x_axis_column, y=y_axis_column))
            + geom_boxplot()
            + facet_grid(facet_dimension) #TODO: Changed from "+ facet_grid(facets="~" + facet_dimension)" due to error
        )
    elif arguments['-g'][0].lower() == "line":
        plot = (
            ggplot(df, aes(x=x_axis_column, y=y_axis_column)) #TODO: Temporarily removed color=color_dimension due to error
            + geom_line()
            + geom_point()
            + facet_grid(facet_dimension) #TODO: Changed from "+ facet_grid(facets="~" + facet_dimension)" due to error
        )

    #Set default output filename and extension if not provided
    if '-o' not in arguments: #if output file not specified, just display
        #output_filename = "output_file.pdf"
        print(plot)
        return
    elif not output_file.endswith('.pdf'): #if not file extension is specified, pdf is the default
        output_file += '.pdf'

    #navigate to output directory if it exists
    current_directory = os.getcwd() #TODO: Added this line to get current directory, then concatenated with output_file in following line
    output_directory = os.path.dirname(current_directory + output_file)
    if output_directory: #if file exists, move to it
        os.makedirs(output_directory, exist_ok=True)
        os.chdir(output_directory)
    else:
        print(f"ERROR: filepath '{output_directory}' does not exist.")
        return

    # Save the plot with the specified file name and extension in the specified directory
    #output_filepath = os.path.join(output_directory, output_file)
    plot.save(output_file, units="in")
    print(f"Plot saved as '{output_file}'")


#main function call
main()