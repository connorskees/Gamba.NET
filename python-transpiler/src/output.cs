public class Parser
{
    public Parser(expr, modulus, modRed)
    {
        self.__expr = expr;
        var foobar = expr;
        self.__modulus = modulus;
        self.__modRed = modRed;
        self.__idx = 0;
        self.__error = "";
    }

    public dynamic parse_expression()
    {
        if (self.__has_space()) {
            self.__skip_space();
        }
        var root = self.__parse_inclusive_disjunction();
        while ((self.__idx < self.__expr.length && self.__has_space())) {
            self.__skip_space();
        }
        if (self.__idx != self.__expr.length) {
            self.__error = "Finished near to " + self.__peek() + " before everything was parsed";
            return null;
        }
        return root;
    }

    public dynamic __new_node(t)
    {
        return new Node(t, self.__modulus, self.__modRed);
    }

    public Node __parse_inclusive_disjunction()
    {
        var child = self.__parse_exclusive_disjunction();
        if (child == null) {
            return null;
        }
        if (self.__peek() != "|") {
            return child;
        }
        var node = self.__new_node(NodeType.INCL_DISJUNCTION);
        node.children.Add(child);
        while (self.__peek() == "|") {
            self.__get();
            child = self.__parse_exclusive_disjunction();
            if (child == null) {
                return null;
            }
            node.children.Add(child);
        }
        return node;
    }

    public Node __parse_exclusive_disjunction()
    {
        var child = self.__parse_conjunction();
        if (child == null) {
            return null;
        }
        if (self.__peek() != "^") {
            return child;
        }
        var node = self.__new_node(NodeType.EXCL_DISJUNCTION);
        node.children.Add(child);
        while (self.__peek() == "^") {
            self.__get();
            child = self.__parse_conjunction();
            if (child == null) {
                return null;
            }
            node.children.Add(child);
        }
        return node;
    }

    public Node __parse_conjunction()
    {
        var child = self.__parse_shift();
        if (child == null) {
            return null;
        }
        if (self.__peek() != "&") {
            return child;
        }
        var node = self.__new_node(NodeType.CONJUNCTION);
        node.children.Add(child);
        while (self.__peek() == "&") {
            self.__get();
            child = self.__parse_shift();
            if (child == null) {
                return null;
            }
            node.children.Add(child);
        }
        return node;
    }

    public dynamic __parse_shift()
    {
        var base = self.__parse_sum();
        if (base == null) {
            return null;
        }
        if (!(self.__has_lshift())) {
            return base;
        }
        var prod = self.__new_node(NodeType.PRODUCT);
        prod.children.Add(base);
        self.__get();
        self.__get();
        var op = self.__parse_sum();
        if (op == null) {
            return null;
        }
        var power = self.__new_node(NodeType.POWER);
        var two = self.__new_node(NodeType.CONSTANT);
        two.constant = 2;
        power.children.Add(two);
        power.children.Add(op);
        prod.children.Add(power);
        if (self.__has_lshift()) {
            self.__error = "Disallowed nested lshift operator near " + self.__peek();
            return null;
        }
        return prod;
    }

    public Node __parse_sum()
    {
        var child = self.__parse_product();
        if (child == null) {
            return null;
        }
        if ((self.__peek() != "+" && self.__peek() != "-")) {
            return child;
        }
        var node = self.__new_node(NodeType.SUM);
        node.children.Add(child);
        while ((self.__peek() == "+" || self.__peek() == "-")) {
            var negative = self.__peek() == "-";
            self.__get();
            child = self.__parse_product();
            if (child == null) {
                return null;
            }
            if (negative) {
                node.children.Add(self.__multiply_by_minus_one(child));
            } else {
                node.children.Add(child);
            }
        }
        return node;
    }

    public Node __parse_product()
    {
        var child = self.__parse_factor();
        if (child == null) {
            return null;
        }
        if (!(self.__has_multiplicator())) {
            return child;
        }
        var node = self.__new_node(NodeType.PRODUCT);
        node.children.Add(child);
        while (self.__has_multiplicator()) {
            self.__get();
            child = self.__parse_factor();
            if (child == null) {
                return null;
            }
            node.children.Add(child);
        }
        return node;
    }

    public dynamic __parse_factor()
    {
        if (self.__has_bitwise_negated_expression()) {
            return self.__parse_bitwise_negated_expression();
        }
        if (self.__has_negative_expression()) {
            return self.__parse_negative_expression();
        }
        return self.__parse_power();
    }

    public Node __parse_bitwise_negated_expression()
    {
        self.__get();
        var node = self.__new_node(NodeType.NEGATION);
        var child = self.__parse_factor();
        if (child == null) {
            return null;
        }
        node.children = new List<dynamic>() { child };
        return node;
    }

    public dynamic __parse_negative_expression()
    {
        self.__get();
        var node = self.__parse_factor();
        if (node == null) {
            return null;
        }
        return self.__multiply_by_minus_one(node);
    }

    public dynamic __multiply_by_minus_one(node)
    {
        if (node.type == NodeType.CONSTANT) {
            node.constant = -(node.constant);
            return node;
        }
        if (node.type == NodeType.PRODUCT) {
            if (node.children[0].type == NodeType.CONSTANT) {
                node.children[0].constant *= -(1)
                return node;
            }
            var minusOne = self.__new_node(NodeType.CONSTANT);
            minusOne.constant = -(1);
            node.children.Insert(0, minusOne);
            return node;
        }
        minusOne = self.__new_node(NodeType.CONSTANT);
        minusOne.constant = -(1);
        var prod = self.__new_node(NodeType.PRODUCT);
        prod.children = new List<dynamic>() { minusOne, node };
        return prod;
    }

    public Node __parse_power()
    {
        var base = self.__parse_terminal();
        if (base == null) {
            return null;
        }
        if (!(self.__has_power())) {
            return base;
        }
        var node = self.__new_node(NodeType.POWER);
        node.children.Add(base);
        self.__get();
        self.__get();
        var exp = self.__parse_terminal();
        if (exp == null) {
            return null;
        }
        node.children.Add(exp);
        if (self.__has_power()) {
            self.__error = "Disallowed nested power operator near " + self.__peek();
            return null;
        }
        return node;
    }

    public dynamic __parse_terminal()
    {
        if (self.__peek() == "(") {
            self.__get();
            var node = self.__parse_inclusive_disjunction();
            if (node == null) {
                return null;
            }
            if (!(self.__peek() == ")")) {
                self.__error = "Missing closing parentheses near to " + self.__peek();
                return null;
            }
            self.__get();
            return node;
        }
        if (self.__has_variable()) {
            return self.__parse_variable();
        }
        return self.__parse_constant();
    }

    public Node __parse_variable()
    {
        var start = self.__idx;
        self.__get(false);
        while ((self.__has_decimal_digit() || self.__has_letter() || self.__peek() == "_")) {
            self.__get(false);
        }
        if (self.__peek() == "[") {
            self.__get(false);
            while (self.__has_decimal_digit()) {
                self.__get(false);
            }
            if (self.__peek() == "]") {
                self.__get();
            } else {
                return null;
            }
        } else {
            while (self.__has_space()) {
                self.__skip_space();
            }
        }
        var node = self.__new_node(NodeType.VARIABLE);
        node.vname = self.__expr[.Slice(start, self.__idx, null)].rstrip();
        return node;
    }

    public dynamic __parse_constant()
    {
        if (self.__has_binary_constant()) {
            return self.__parse_binary_constant();
        }
        if (self.__has_hex_constant()) {
            return self.__parse_hex_constant();
        }
        return self.__parse_decimal_constant();
    }

    public Node __parse_binary_constant()
    {
        self.__get(false);
        self.__get(false);
        if (!((new List<dynamic>() { "0", "1" }).Contains(self.__peek()))) {
            self.__error = "Invalid binary digit near to " + self.__peek();
            return null;
        }
        var start = self.__idx;
        while (((new List<dynamic>() { "0", "1" }).Contains(self.__peek()))) {
            self.__get(false);
        }
        while (self.__has_space()) {
            self.__skip_space();
        }
        var node = self.__new_node(NodeType.CONSTANT);
        node.constant = self.__get_constant(start, 2);
        return node;
    }

    public Node __parse_hex_constant()
    {
        self.__get(false);
        self.__get(false);
        if (!(self.__has_hex_digit())) {
            self.__error = "Invalid hex digit near to " + self.__peek();
            return null;
        }
        var start = self.__idx;
        while (self.__has_hex_digit()) {
            self.__get(false);
        }
        while (self.__has_space()) {
            self.__skip_space();
        }
        var node = self.__new_node(NodeType.CONSTANT);
        node.constant = self.__get_constant(start, 16);
        return node;
    }

    public Node __parse_decimal_constant()
    {
        if (!(self.__has_decimal_digit())) {
            self.__error = "Expecting constant at " + self.__peek() + ", but no digit around.";
            return null;
        }
        var start = self.__idx;
        while (self.__has_decimal_digit()) {
            self.__get(false);
        }
        while (self.__has_space()) {
            self.__skip_space();
        }
        var node = self.__new_node(NodeType.CONSTANT);
        node.constant = self.__get_constant(start, 10);
        return node;
    }

    public dynamic __get_constant(start, base)
    {
        return int(self.__expr[.Slice(start, self.__idx, null)].rstrip(), base);
    }

    public dynamic __get(skipSpace)
    {
        var c = self.__peek();
        self.__idx += 1
        if (skipSpace) {
            while (self.__has_space()) {
                self.__skip_space();
            }
        }
        return c;
    }

    public void __skip_space()
    {
        self.__idx += 1
    }

    public dynamic __peek()
    {
        if (self.__idx >= self.__expr.length) {
            return "";
        }
        return self.__expr[self.__idx];
    }

    public dynamic __has_bitwise_negated_expression()
    {
        return self.__peek() == "~";
    }

    public dynamic __has_negative_expression()
    {
        return self.__peek() == "-";
    }

    public dynamic __has_multiplicator()
    {
        return (self.__peek() == "*" && self.__peek_next() != "*");
    }

    public dynamic __has_power()
    {
        return (self.__peek() == "*" && self.__peek_next() == "*");
    }

    public dynamic __has_lshift()
    {
        return (self.__peek() == "<" && self.__peek_next() == "<");
    }

    public dynamic __has_binary_constant()
    {
        return (self.__peek() == "0" && self.__peek_next() == "b");
    }

    public dynamic __has_hex_constant()
    {
        return (self.__peek() == "0" && self.__peek_next() == "x");
    }

    public dynamic __has_hex_digit()
    {
        return (self.__has_decimal_digit() || re.match("[a-fA-F]", self.__peek()));
    }

    public dynamic __has_decimal_digit()
    {
        return re.match("[0-9]", self.__peek());
    }

    public dynamic __has_variable()
    {
        return self.__has_letter();
    }

    public dynamic __has_letter()
    {
        return re.match("[a-zA-Z]", self.__peek());
    }

    public dynamic __has_space()
    {
        return self.__peek() == " ";
    }

    public dynamic __peek_next()
    {
        if (self.__idx >= self.__expr.length - 1) {
            return "";
        }
        return self.__expr[self.__idx + 1];
    }

}


