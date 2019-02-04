$input = $args[0]
$output = $args[1]

echo $args

Get-ChildItem -Recurse $input *.ojn | ForEach{
	$process = $_.FullName
	echo "Processing $process"
	.\O2JamCLI.exe -i $process -o $output -z -f
	echo ""
}

