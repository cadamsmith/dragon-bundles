var Calculator = function () {
    this.total = 0;
};
Calculator.prototype.add = function (value) {
    this.total = this.total + value;
    return this.total;
};
function sum(numbers) {
    var result = 0;
    for (var i = 0; i < numbers.length; i++) {
        result = result + numbers[i];
    }
    return result;
}
