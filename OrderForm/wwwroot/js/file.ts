//import 'jquery';

//function ValidateTextInput(target: HTMLInputElement) {
//	if (target.type == "text") {
//		return;
//	}
//	let filterPattern = new RegExp(target.attributes.getNamedItem("filterPattern").value, 'g');
//	let filteredValue = target.value.replace(filterPattern, '');
//	let matchPattern = new RegExp(target.attributes.getNamedItem("formatPattern").value);
//	if (matchPattern.test(filteredValue)) {
//		target.value = filteredValue;
//		let arr = filteredValue.match(matchPattern);
//	}
//}