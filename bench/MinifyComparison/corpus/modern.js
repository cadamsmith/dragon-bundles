class Calculator {
    #total = 0;
    add = (value) => {
        this.#total += value;
        return this.#total;
    };
}
const sum = (...numbers) => numbers.reduce((acc, n) => acc + n, 0);
const config = { timeout: 5000, retries: 3 };
const timeout = config?.timeout ?? 1000;
const greet = (name) => `Hello, ${name}! You have ${sum(1, 2, 3)} points.`;
async function load(url) {
    const res = await fetch(url);
    return res?.ok ? await res.json() : null;
}
